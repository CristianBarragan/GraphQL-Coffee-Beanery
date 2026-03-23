using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using FASTER.core;
using HotChocolate.Execution.Processing;
using Npgsql;

namespace CoffeeBeanery.Service
{
    public interface IProcessService<M>
    {
        Task<QueryResult> QueryProcessAsync(string cacheKey, ISelection selection, string modelName, string wrapperName, CancellationToken cancellationToken);

        Task<QueryResult> MutationProcessAsync(string cacheKey, ISelection selection, string modelName, string wrapperName, CancellationToken cancellationToken);
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

        public async Task<QueryResult> QueryProcessAsync(
            string cacheKey, ISelection selection, string modelName, string wrapperName, CancellationToken cancellationToken)
        {
            var ctx = new SqlCompilationContext();
            var transformedToParent = false;
            var entityName = string.Empty;
            
            while (!SqlNodeRegistry.EntityNames.Contains(entityName) || entityName.Matches(wrapperName))
            {
                entityName = SqlNodeRegistry.ModelTrees[modelName].ParentName;
                transformedToParent = true;
            }

            var rootTree = SqlNodeRegistry.EntityTrees[modelName];

            var edgeStatementNodes = new Dictionary<string, SqlNode>();
            var visitedModels = new List<string>();
            visitedModels.Add(SqlNodeRegistry.ModelTrees.Values.OrderBy(a => a.Id).First().Name);
            var visitedEntities = new List<string>();
            var nodeStatementNodes = new Dictionary<string, SqlNode>();

            SqlNodeResolver.GetFields(SqlNodeRegistry.ModelTrees, SqlNodeRegistry.EntityTrees, selection.SyntaxNode, SqlNodeRegistry.EntityNodes,
                SqlNodeRegistry.ModelNodes, edgeStatementNodes, rootTree, new NodeTree(), visitedModels, visitedEntities,
                SqlNodeRegistry.ModelNames, SqlNodeRegistry.EntityNames, true);

            var rootEdgeEntity = SqlNodeRegistry.EntityTrees.OrderBy(a => a.Value.Id)
                .First(a => visitedEntities.Contains(a.Key));
            visitedEntities.Clear();
            
            visitedEntities.Clear();
            visitedModels.Add(SqlNodeRegistry.ModelTrees.Values.OrderBy(a => a.Id).First().Name);

            SqlNodeResolver.GetFields(SqlNodeRegistry.ModelTrees, SqlNodeRegistry.EntityTrees, selection.SyntaxNode, SqlNodeRegistry.EntityNodes,
                SqlNodeRegistry.ModelNodes, nodeStatementNodes, rootTree, new NodeTree(), visitedModels, visitedEntities,
                SqlNodeRegistry.ModelNames, SqlNodeRegistry.EntityNames, false);

            var rootNodeEntity = SqlNodeRegistry.EntityTrees.OrderBy(a => a.Value.Id)
                .First(a => visitedEntities.Contains(a.Key));

            // var rootEntity = string.Empty;
            //
            // if (rootEdgeEntity.Key != null && rootNodeEntity.Key == null)
            // {
            //     rootEntity = rootEdgeEntity.Key;
            // }
            // else if (rootEdgeEntity.Key == null && rootNodeEntity.Key != null)
            // {
            //     rootEntity = rootNodeEntity.Key;
            // }
            // else if (rootNodeEntity.Key != null && rootEdgeEntity.Key != null)
            // {
            //     rootEntity = int.Parse(rootEdgeEntity.Value.Id) > int.Parse(rootNodeEntity.Value.Id)
            //         ? rootNodeEntity.Value.Name
            //         : rootEdgeEntity.Value.Name;
            // }

            var sqlWhereStatement = new Dictionary<string, string>();

            // Compile SQL using your current compiler
            var sqlStructure = SqlQueryCompiler.Compile(
                selection,
                rootTree,
                edgeStatementNodes,
                nodeStatementNodes,
                rootNodeEntity.Key,
                SqlNodeRegistry.EntityTrees,
                sqlWhereStatement,
                transformedToParent
            );

            var parameters = new ProcessQueryParameters
            {
                SqlStructure = sqlStructure,
                Pagination = ctx.Pagination
            };

            var (models, startCursor, endCursor, totalCount, totalPageRecords) =
                await _queryDispatcher
                    .DispatchAsync<ProcessQueryParameters, (List<M> Process, int? startCursor, int? endCursor, int?
                        totalCount, int? totalPageRecords)>(parameters, cancellationToken);

            return new QueryResult
            {
                Models = models,
                StartCursor = startCursor,
                EndCursor = endCursor,
                TotalCount = totalCount ?? 0,
                TotalPageRecords = totalPageRecords ?? 0
            };
        }

        public async Task<QueryResult> MutationProcessAsync(
            string cacheKey, ISelection selection, string modelName, string wrapperName, CancellationToken cancellationToken)
        {
            var ctx = new SqlCompilationContext();
            var transformedToParent = false;
            var entityName = modelName;
            
            while (!SqlNodeRegistry.EntityNames.Contains(entityName) || entityName.Matches(wrapperName))
            {
                if (SqlNodeRegistry.ModelTrees.ContainsKey(entityName))
                {
                    entityName = SqlNodeRegistry.ModelTrees[entityName].ParentName;

                    if (string.IsNullOrEmpty(entityName))
                    {
                        entityName = SqlNodeRegistry.EntityTrees.OrderBy(a => a.Value.Id).First().Key;
                    }
                
                    transformedToParent = true;    
                }
            }

            var rootTree = SqlNodeRegistry.EntityTrees[entityName];

            var mutationStatementNodes = new Dictionary<string, SqlNode>();

            var argument = selection.SyntaxNode.Arguments.FirstOrDefault(a => a.Name.Value.Matches(wrapperName));

            if (
                argument.GetNodes().ToList()[1].ToString().StartsWith("["))
            {
                var mutationNodeToProcess = argument.Value.GetNodes()
                    .First(a => !a.ToString().Contains("cache") && !a.ToString().Contains("model"));

                foreach (var mutationNode in mutationNodeToProcess.GetNodes().ToList()[1].GetNodes())
                {
                    SqlNodeResolver.GetMutations(SqlNodeRegistry.ModelTrees, mutationNode, SqlNodeRegistry.EntityNodes,
                        SqlNodeRegistry.ModelNodes, mutationStatementNodes,
                        rootTree, string.Empty, rootTree, SqlNodeRegistry.ModelTrees.Keys.ToList(), new List<string>());
                }
            }
            else
            {
                SqlNodeResolver.GetMutations(SqlNodeRegistry.ModelTrees, selection.SyntaxNode.Arguments[0], SqlNodeRegistry.EntityNodes,
                    SqlNodeRegistry.ModelNodes, mutationStatementNodes,
                    rootTree, string.Empty, rootTree, SqlNodeRegistry.ModelTrees.Keys.ToList(), new List<string>());
            }

            var sqlWhereStatement = new Dictionary<string, string>();
            
            var mutationStructure = SqlMutationCompiler.Compile(selection, SqlNodeRegistry.ModelTrees[modelName], wrapperName, mutationStatementNodes, sqlWhereStatement);

            var edgeStatementNodes = new Dictionary<string, SqlNode>();
            var visitedModels = new List<string>();
            visitedModels.Add(SqlNodeRegistry.ModelTrees.Values.OrderBy(a => a.Id).First().Name);
            var visitedEntities = new List<string>();
            var nodeStatementNodes = new Dictionary<string, SqlNode>();

            SqlNodeResolver.GetFields(SqlNodeRegistry.ModelTrees, SqlNodeRegistry.EntityTrees, selection.SyntaxNode.GetNodes().ToList()[2], SqlNodeRegistry.EntityNodes,
                SqlNodeRegistry.ModelNodes, edgeStatementNodes, rootTree, new NodeTree(), visitedModels, visitedEntities,
                SqlNodeRegistry.ModelNames, SqlNodeRegistry.EntityNames, true);

            var rootEdgeEntity = SqlNodeRegistry.EntityTrees.OrderBy(a => a.Value.Id)
                .FirstOrDefault(a => visitedEntities.Contains(a.Key));
            visitedEntities.Clear();
            visitedModels.Clear();
            visitedModels.Add(SqlNodeRegistry.ModelTrees.Values.OrderBy(a => a.Id).First().Name);

            if (edgeStatementNodes.Count == 0)
            {
                rootEdgeEntity = default;
            }

            SqlNodeResolver.GetFields(SqlNodeRegistry.ModelTrees, SqlNodeRegistry.EntityTrees, selection.SyntaxNode.GetNodes().ToList()[2], SqlNodeRegistry.EntityNodes,
                SqlNodeRegistry.ModelNodes, nodeStatementNodes, rootTree, new NodeTree(), visitedModels, visitedEntities,
                SqlNodeRegistry.ModelNames, SqlNodeRegistry.EntityNames, false);

            var rootNodeEntity = SqlNodeRegistry.EntityTrees.OrderBy(a => a.Value.Id)
                .FirstOrDefault(a => visitedEntities.Contains(a.Key));

            if (nodeStatementNodes.Count == 0)
            {
                rootNodeEntity = default;
            }

            var rootEntity = string.Empty;

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
                    ? rootNodeEntity.Value.Name
                    : rootEdgeEntity.Value.Name;
            }

            // Compile SQL using your current compiler
            var sqlStructure = SqlQueryCompiler.Compile(
                selection,
                rootTree,
                edgeStatementNodes,
                nodeStatementNodes,
                rootEntity,
                SqlNodeRegistry.EntityTrees,
                sqlWhereStatement,
                transformedToParent
            );
            
            sqlStructure.SqlUpsert = mutationStructure.SqlUpsert;

            var parameters = new ProcessQueryParameters
            {
                SqlStructure = sqlStructure,
                Pagination = ctx.Pagination
            };

            var (models, startCursor, endCursor, totalCount, totalPageRecords) =
                await _queryDispatcher
                    .DispatchAsync<ProcessQueryParameters, (List<M> Process, int? startCursor, int? endCursor, int?
                        totalCount, int? totalPageRecords)>(parameters, cancellationToken);

            return new QueryResult
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
