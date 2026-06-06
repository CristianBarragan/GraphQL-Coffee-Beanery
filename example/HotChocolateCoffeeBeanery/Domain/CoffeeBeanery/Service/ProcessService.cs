using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using FASTER.core;
using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.Service
{
    public interface IProcessService<M> where M : class
    {
        Task<QueryResult<M>> QueryProcessAsync(
            string cacheKey, ISelection selection,
            string modelName,
            CancellationToken cancellationToken);

        Task<QueryResult<M>> MutationProcessAsync(
            string cacheKey, ISelection selection,
            string modelName,
            CancellationToken cancellationToken);
    }

    public class ProcessService<M> : IProcessService<M>
        where M : class
    {
        private readonly IQueryDispatcher _queryDispatcher;
        private IFasterKV<string, string> _cache;

        public ProcessService(
            IQueryDispatcher queryDispatcher,
            IFasterKV<string, string> cache)
        {
            _queryDispatcher = queryDispatcher;
            _cache = cache;
        }

        public async Task<QueryResult<M>> QueryProcessAsync(
            string cacheKey, ISelection selection, string modelName, CancellationToken cancellationToken)
        {
            var ctx = new SqlCompilationContext();
            
            var rootTree = SqlNodeRegistry.ModelTrees.First(a => 
                a.Value.ModelName.Matches(modelName)).Value;
            
            var sqlStructure = SqlQueryCompiler.Compile(
                selection,
                rootTree,
                SqlNodeRegistry.EntityNodes,
                SqlNodeRegistry.EntityTrees,
                SqlNodeRegistry.ModelTrees,
                _cache,
                cacheKey
            );
            
            sqlStructure.ModelTrees = SqlNodeRegistry.ModelTrees;
            sqlStructure.EntityTrees = SqlNodeRegistry.EntityTrees;
            sqlStructure.RelativeTree = rootTree;
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

        public async Task<QueryResult<M>> MutationProcessAsync(
            string cacheKey, ISelection selection, string modelName, CancellationToken cancellationToken)
        {
            var ctx = new SqlCompilationContext();
            
            var rootTree = SqlNodeRegistry.ModelTrees.First(a => 
                a.Value.ModelName.Matches(modelName)).Value;
            
            var sqlWhereStatement = new Dictionary<string, string>();

            var mutationStructure = SqlMutationCompiler.Compile(selection, rootTree, sqlWhereStatement, SqlNodeRegistry.ModelTrees, SqlNodeRegistry.EntityTrees, SqlNodeRegistry.ModelNodes, 
                SqlNodeRegistry.EntityNodes, SqlNodeRegistry.ModelNames, SqlNodeRegistry.EntityNames);

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
            sqlStructure.RelativeTree = rootTree;
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
