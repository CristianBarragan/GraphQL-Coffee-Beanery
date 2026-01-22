using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.GraphQL.Core.Sql;
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
        private readonly IDynamicQueryHandler _queryHandler;

        public ProcessService(
            ILoggerFactory loggerFactory,
            NpgsqlConnection dbConnection,
            IDynamicQueryHandler queryHandler)
        {
            _logger = loggerFactory.CreateLogger<ProcessService<M>>();
            _dbConnection = dbConnection;
            _queryHandler = queryHandler;
        }

        public async Task<QueryResult> QueryProcessAsync(
            string cacheKey, ISelection selection, string modelName, string wrapperName, CancellationToken cancellationToken)
        {
            var ctx = new SqlCompilationContext();

            var rootNode = new NodeTree { Name = modelName };

            // Compile SQL using your current compiler
            var sqlStructure = SqlCompiler.Compile(
                selection,
                rootNode,
                SqlNodeRegistry.EdgeNodes,
                SqlNodeRegistry.NodeNodes,
                SqlNodeRegistry.MutationNodes
            );

            var parameters = new ProcessQueryParameters
            {
                SqlStructure = new SqlStructure
                {
                    SqlQuery = $"{sqlStructure.SqlUpsert};{sqlStructure.SqlQuery}"
                },
                Pagination = ctx.Pagination
            };

            var (models, startCursor, endCursor, totalCount, totalPageRecords) =
                await _queryHandler.ExecuteAsync(parameters, cancellationToken);

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
            string cacheKey, ISelection selection, string rootName, string wrapperName,
            CancellationToken cancellationToken)
        {
            var ctx = new SqlCompilationContext();

            var rootTree = NodeTreeBuilder.Build(rootName, SqlNodeRegistry.EntityTrees);

            var statementNodes = new Dictionary<string, SqlNode>();
            
            SqlNodeResolver.GetMutations(SqlNodeRegistry.EntityTrees, selection.SyntaxNode, SqlNodeRegistry.NodeNodes, SqlNodeRegistry.NodeNodes, statementNodes,
                rootTree, string.Empty, rootTree, new List<string>(), new List<string>(), new List<string>());

            var mutationStructure = SqlMutationCompiler.Compile(
                selection,
                rootTree,
                SqlNodeRegistry.EdgeNodes,
                SqlNodeRegistry.NodeNodes,
                SqlNodeRegistry.MutationNodes
            );
            
            // Compile SQL using your current compiler
            var sqlStructure = SqlCompiler.Compile(
                selection,
                rootTree,
                SqlNodeRegistry.EdgeNodes,
                SqlNodeRegistry.NodeNodes,
                SqlNodeRegistry.MutationNodes
            );

            var parameters = new ProcessQueryParameters
            {
                SqlStructure = new SqlStructure
                {
                    SqlQuery = sqlStructure.SqlQuery,
                    SqlUpsert = mutationStructure.SqlUpsert
                },
                Pagination = ctx.Pagination
            };

            var (models, startCursor, endCursor, totalCount, totalPageRecords) =
                await _queryHandler.ExecuteAsync(parameters, cancellationToken);

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
