using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using FASTER.core;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using Npgsql;

namespace CoffeeBeanery.Service
{
    // IProcessService.cs
    public interface IProcessService<M> where M : class
    {
        Task<QueryResult<M>> QueryProcessAsync(
            string cacheKey, ISelection selection,
            string modelName, string wrapperName,
            CancellationToken cancellationToken);

        Task<QueryResult<M>> MutationProcessAsync(
            string cacheKey, ISelection selection,
            string modelName, string rootName, string wrapperName,
            CancellationToken cancellationToken);
    }

    public class ProcessService<M> : IProcessService<M>
        where M : class
    {
        private readonly ILogger _logger;
        private readonly NpgsqlConnection _dbConnection;
        private readonly IQueryDispatcher _queryDispatcher;
        private IFasterKV<string, string> _cache;

        public ProcessService(
            ILoggerFactory loggerFactory,
            NpgsqlConnection dbConnection,
            IQueryDispatcher queryDispatcher,
            IFasterKV<string, string> cache)
        {
            _logger = loggerFactory.CreateLogger<ProcessService<M>>();
            _dbConnection = dbConnection;
            _queryDispatcher = queryDispatcher;
            _cache = cache;
        }

        public async Task<QueryResult<M>> QueryProcessAsync(
            string cacheKey, ISelection selection, string modelName, string wrapperName, CancellationToken cancellationToken)
        {
            var ctx = new SqlCompilationContext();
            var transformedToParent = false;
            
            var entityName = SqlNodeRegistry.EntityTrees.FirstOrDefault(a => a.Value.ModelToEntityLinks.Any(b=> b.From.Matches(modelName))).Value.Parents[0].To;

            var rootTree = SqlNodeRegistry.EntityTrees[modelName];
            
            var edgeStatementNodes = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);
            var visitedModels = new List<string>();
            var visitedEntities = new List<string>();
            var nodeStatementNodes = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);

            SqlNodeResolver.GetFields(SqlNodeRegistry.ModelTrees, SqlNodeRegistry.EntityTrees, selection.SyntaxNode.GetNodes()
                    .ToList().Last(a => a.Kind == SyntaxKind.SelectionSet).GetNodes().ToList().Last().GetNodes().Last().GetNodes().First(), 
                SqlNodeRegistry.EntityNodes, SqlNodeRegistry.ModelNodes, edgeStatementNodes, rootTree, new NodeTree(), visitedModels, 
                visitedEntities, SqlNodeRegistry.ModelNames, SqlNodeRegistry.EntityNames, true);

            var rootNodeEntity = SqlNodeRegistry.EntityTrees.OrderBy(a => a.Value.Id)
                .FirstOrDefault(a => visitedEntities.Contains(a.Key));

            if (nodeStatementNodes.Count == 0)
            {
                rootNodeEntity = default;
            }
            
            SqlNodeResolver.GetFields(SqlNodeRegistry.ModelTrees, SqlNodeRegistry.EntityTrees, selection.SyntaxNode.GetNodes().ToList().Last(a => a.Kind == SyntaxKind.SelectionSet), SqlNodeRegistry.EntityNodes,
                SqlNodeRegistry.ModelNodes, nodeStatementNodes, rootTree, new NodeTree(), visitedModels, visitedEntities,
                SqlNodeRegistry.ModelNames, SqlNodeRegistry.EntityNames, false);
            
            var rootEdgeEntity = SqlNodeRegistry.EntityTrees.OrderBy(a => a.Value.Id)
                .FirstOrDefault(a => visitedEntities.Contains(a.Key));
            visitedEntities.Clear();
            visitedModels.Clear();

            if (edgeStatementNodes.Count == 0)
            {
                rootEdgeEntity = default;
            }

            var rootEntity = modelName;

            if (rootEdgeEntity.Key != null && rootNodeEntity.Key == null)
            {
                rootEntity = rootEdgeEntity.Key;
            }
            else if (rootEdgeEntity.Key == null && rootNodeEntity.Key != null)
            {
                rootEntity = rootNodeEntity.Key;
            }
            else if (rootNodeEntity.Key != null && rootEdgeEntity.Key != null)
            {
                rootEntity = rootEdgeEntity.Value.Id > rootNodeEntity.Value.Id
                    ? rootNodeEntity.Key
                    : rootEdgeEntity.Key;
            }

            // Compile SQL using your current compiler
            // var sqlStructure = SqlQueryCompiler.Compile(
            //     selection,
            //     rootTree,
            //     edgeStatementNodes,
            //     nodeStatementNodes,
            //     rootEntity,
            //     SqlNodeRegistry.EntityTrees,
            //     SqlNodeRegistry.ModelTrees,
            //     sqlWhereStatement,
            //     _cache,
            //     wrapperName,
            //     cacheKey,
            //     modelName
            // );

            var parameters = new ProcessQueryParameters
            {
                // SqlStructure = sqlStructure,
                Pagination = ctx.Pagination,
                Model = modelName
            };

            var (models, startCursor, endCursor, totalCount, totalPageRecords) =
                await _queryDispatcher
                    .DispatchAsync<ProcessQueryParameters, (List<M>, int?, int?, int?, int?)>(
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
            string cacheKey, ISelection selection, string modelName, string rootName, string wrapperName, CancellationToken cancellationToken)
        {
            var ctx = new SqlCompilationContext();
            var transformedToParent = false;
            
            var rootTree = SqlNodeRegistry.ModelTrees.First(a => 
                a.Value.ModelName.Matches(modelName)).Value;
            
            var sqlWhereStatement = new Dictionary<string, string>();

            var mutationStructure = SqlMutationCompiler.Compile(selection, rootTree, sqlWhereStatement, SqlNodeRegistry.ModelTrees, SqlNodeRegistry.EntityTrees, SqlNodeRegistry.ModelNodes, 
                SqlNodeRegistry.EntityNodes, SqlNodeRegistry.ModelNames, SqlNodeRegistry.EntityNames);

            // Compile SQL using your current compiler
            var sqlStructure = SqlQueryCompiler.Compile(
                selection,
                rootTree,
                SqlNodeRegistry.EntityNodes,
                SqlNodeRegistry.EntityTrees,
                SqlNodeRegistry.ModelTrees,
                _cache,
                cacheKey
            );
            
            sqlStructure.SqlUpsert = mutationStructure.SqlUpsert;
            sqlStructure.ModelTrees = SqlNodeRegistry.ModelTrees;
            sqlStructure.EntityTrees = SqlNodeRegistry.EntityTrees;
            sqlStructure.SqlNodes = SqlNodeRegistry.EntityNodes.Select(a => a.Value).ToArray();

            var parameters = new ProcessQueryParameters
            {
                SqlStructure = sqlStructure,
                Pagination = ctx.Pagination,
                Model = modelName
            };

            var (models, startCursor, endCursor, totalCount, totalPageRecords) =
                await _queryDispatcher
                    .DispatchAsync<ProcessQueryParameters, (List<M>, int?, int?, int?, int?)>(
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

    }
}
