using CoffeeBeanery.GraphQL.Core.Compiler;
using CoffeeBeanery.GraphQL.Core.Mapping;
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

            var resolved = SqlNodeResolver.ResolveFromSelection<M>(selection, wrapperName, false);

            SqlWhereBuilder.BuildWhere<M>(ctx, selection, resolved.select, wrapperName);
            SqlOrderByBuilder.BuildOrderBy(ctx, selection, resolved.select, wrapperName);
            var pagination = SqlPaginationBuilder.BuildPagination(ctx, selection);

            var rootNode = new NodeTree { Name = wrapperName };

            var sqlQuery = SqlSelectGenerator.BuildSelect(ctx, resolved.select, rootNode);

            var parameters = new ProcessQueryParameters
            {
                SqlStructure = new SqlStructure
                {
                    Sql = sqlQuery.Query
                },
                Pagination = pagination,
                SplitOnDapper = sqlQuery.SplitOnDapper
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
            string cacheKey, ISelection selection, string modelName, string wrapperName,
            CancellationToken cancellationToken)
        {
            var ctx = new SqlCompilationContext();

            var resolved = SqlNodeResolver.ResolveFromSelection<M>(selection, wrapperName, true);

            var rootNode = new NodeTree { Name = wrapperName };

            var sqlMutation = SqlMutationGenerator.BuildMutation(ctx, resolved.mutation, rootNode);

            // build select part to return inserted values
            var sqlQuery = SqlSelectGenerator.BuildSelect(ctx, resolved.select, rootNode);

            var pagination = SqlPaginationBuilder.BuildPagination(ctx, selection);

            var parameters = new ProcessQueryParameters
            {
                SqlStructure = new SqlStructure
                {
                    Sql = $"{sqlMutation};{sqlQuery}"
                },
                Pagination = pagination
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
