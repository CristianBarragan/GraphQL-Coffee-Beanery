using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.CoreNew.Compiler
{
    public static class SqlMutationCompilerNew
    {
        public static string CompileMutation<TModel>(
            ISelection rootSelection,
            string rootEntity)
            where TModel : class
        {
            var ctx = new SqlCompilationContext();

            var nodes = SqlNodeResolverNew.ResolveFromSelection<TModel>(
                rootSelection, rootEntity, true);

            return SqlMutationGeneratorNew.BuildUpserts(ctx, nodes, rootEntity);
        }
    }
}
