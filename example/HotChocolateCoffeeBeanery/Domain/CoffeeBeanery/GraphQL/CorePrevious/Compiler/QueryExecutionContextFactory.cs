using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class QueryExecutionContextFactory
    {
        public static QueryExecutionContext BuildForQuery<TModel>(
            ISelection selection,
            NodeTree nodeEntity)
            where TModel : class
        {
            var ctx = new QueryExecutionContext
            {
                SqlQuery = SqlQueryCompiler.CompileQuery<TModel>(
                    selection, nodeEntity)
            };

            // These assignments now work because properties are mutable
            ctx.SplitOnDapper[nodeEntity.Name] = nodeEntity.Name;
            ctx.SplitOnTypes[nodeEntity.Name] = typeof(TModel);

            return ctx;
        }

        public static QueryExecutionContext BuildForMutation<TModel>(
            ISelection selection,
            string rootEntityName)
            where TModel : class
        {
            var ctx = new QueryExecutionContext
            {
                SqlUpsert = SqlMutationCompiler.CompileMutation<TModel>(
                    selection, rootEntityName)
            };

            return ctx;
        }
    }
}