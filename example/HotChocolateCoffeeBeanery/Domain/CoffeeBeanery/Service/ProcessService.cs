using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using CoffeeBeanery.Service.Materialization;
using Dapper;
using FASTER.core;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using Npgsql;

namespace CoffeeBeanery.Service
{
    public interface IProcessService<M> where M : class
    {
        Task<QueryResult<M>> QueryProcessAsync(
            string cacheKey,
            ISelection selection,
            string modelName,
            CancellationToken cancellationToken);

        Task<QueryResult<M>> MutationProcessAsync(
            string cacheKey,
            ISelection selection,
            string modelName,
            CancellationToken cancellationToken);
    }

    public class ProcessService<M> : IProcessService<M>
        where M : class
    {
        private readonly IQueryDispatcher _queryDispatcher;
        private readonly NpgsqlDataSource _dataSource;
        private readonly IFasterKV<string, string> _cache;

        public ProcessService(
            IQueryDispatcher queryDispatcher,
            NpgsqlDataSource dataSource,
            IFasterKV<string, string> cache)
        {
            _queryDispatcher = queryDispatcher;
            _dataSource = dataSource;
            _cache = cache;
        }

        public async Task<QueryResult<M>> QueryProcessAsync(
            string cacheKey,
            ISelection selection,
            string modelName,
            CancellationToken cancellationToken)
        {
            var rootTree = NodeRegistry.ModelTrees.First(a =>
                a.Value.ModelName.Matches(modelName)).Value;

            var context = new SqlCompilationContext();

            SqlQueryCompiler.Compile(
                context,
                selection,
                NodeRegistry.EntityTrees.First(a =>
                    a.Value.Alias.Matches(rootTree.Alias)).Value,
                _cache,
                cacheKey
            );

            context.ModelTrees = NodeRegistry.ModelTrees;
            context.EntityTrees = NodeRegistry.EntityTrees;
            context.RelativeTree = rootTree;
            context.EntityNodesApplied = NodeRegistry.EntityNodes;

            var parameters = new ProcessQueryParameters
            {
                Context = context,
                Model = modelName
            };

            var (models, startCursor, endCursor, totalCount, totalPageRecords) =
                await _queryDispatcher.DispatchAsync<
                    ProcessQueryParameters,
                    (List<M>, int?, int?, int?, int?)>(
                    parameters, cancellationToken);

            return new QueryResult<M>
            {
                Models = models,
                StartCursor = startCursor,
                EndCursor = endCursor,
                TotalCount = totalCount ?? 0,
                TotalPageRecords = totalPageRecords ?? 0
            };
        }

        public async Task<QueryResult<M>> MutationProcessAsync(
            string cacheKey,
            ISelection selection,
            string modelName,
            CancellationToken cancellationToken)
        {
            var rootTree = NodeRegistry.ModelTrees.First(a =>
                a.Value.ModelName.Matches(modelName)).Value;

            var entityTree = NodeRegistry.EntityTrees.First(a =>
                a.Value.ModelName.Matches(rootTree.ModelName)).Value;

            var sqlWhereStatement = new Dictionary<string, string>();
            var sqlOrderStatement = new Dictionary<string, string>();
            var context = new SqlCompilationContext();

            // ------------------------------------------------------------
            // 1. Mutation compilation
            // ------------------------------------------------------------
            // SqlMutationCompiler.Compile(
            //     context,
            //     selection,
            //     entityTree,
            //     sqlWhereStatement,
            //     NodeRegistry.ModelTrees,
            //     NodeRegistry.EntityTrees,
            //     NodeRegistry.ModelNodes,
            //     NodeRegistry.EntityNodes,
            //     NodeRegistry.ModelTrees.Select(a => a.Value.Name).ToList(),
            //     NodeRegistry.EntityTrees.Select(a => a.Value.Name).ToList());

            // ------------------------------------------------------------
            // 2. Mutation graph walk (write side)
            // ------------------------------------------------------------
            var mutationArgument = selection.SyntaxNode.Arguments
                .FirstOrDefault(a =>
                    !a.Name.Value.Matches("where") &&
                    !a.Name.Value.Matches("order"));

            var mutationData = new MutationGraphWalker.Result();

            if (mutationArgument?.Value is ObjectValueNode wrapperObj)
            {
                var rootField = wrapperObj.Fields
                    .FirstOrDefault(f => f.Value is ObjectValueNode or ListValueNode);

                if (rootField is not null)
                    mutationData = MutationGraphWalker.Walk(entityTree.Alias, rootField.Value);
            }

            var statements = new List<string>();
            var graphMergeStatements = new List<string>();

            SqlHelper.GenerateUpsertStatements(
                NodeRegistry.EntityTrees,
                mutationData,
                entityTree,
                sqlWhereStatement,
                statements,
                graphMergeStatements);

            // ------------------------------------------------------------
            // 3. Read-side selection walk
            // ------------------------------------------------------------
            var selectionSet = selection.SyntaxNode.SelectionSet;

            var queryData = QueryGraphWalker.Walk(entityTree.Alias, selectionSet);
            
            SqlWhereCompiler.Compile(
                context,
                NodeRegistry.EntityTrees,
                selection,
                entityTree,
                entityTree.Name,
                sqlWhereStatement);

            var select = SqlSelectStatementBuilder.Build(
                NodeRegistry.EntityTrees,
                queryData,
                entityTree,
                sqlWhereStatement,
                sqlOrderStatement);

            // ------------------------------------------------------------
            // 4. Batch execution
            // ------------------------------------------------------------
            var sqlBlocks = new List<string>();
            sqlBlocks.AddRange(statements.Select(s => s.Trim()));
            sqlBlocks.AddRange(graphMergeStatements
                .Select(s => s.TrimStart(';', ' ', '\n', '\r').Trim()));

            var batchSql = sqlBlocks.Count > 0
                ? string.Join("\n", sqlBlocks) + "\n" + select.Sql
                : select.Sql;

            using var connection = await AgeConnectionFactory.OpenAsync(_dataSource);

            using var grid = await connection.QueryMultipleAsync(
                new CommandDefinition(batchSql, cancellationToken: cancellationToken));

            var rawRows = grid.Read(
                select.Types.ToArray(),
                objs => objs,
                splitOn: select.SplitOn);

            var rows = rawRows.Select(o => (object?[])o).ToList();

            var models = DynamicGraphMaterializer.Materialize<M>(
                entityTree.Alias,
                select.AliasOrder,
                rows);

            return new QueryResult<M>
            {
                Models = models,
                StartCursor = null,
                EndCursor = null,
                TotalCount = models.Count,
                TotalPageRecords = models.Count
            };
        }
    }
}