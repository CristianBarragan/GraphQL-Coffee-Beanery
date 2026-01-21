using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SqlMutationCompiler
    {
        public static string CompileMutation<TModel>(
            ISelection rootSelection,
            string rootEntity)
            where TModel : class
        {
            try
            {
                var ctx = new SqlCompilationContext();
                var nodes = SqlNodeResolver.ResolveFromSelection<TModel>(rootSelection, rootEntity, true);

                return SqlMutationPlanner.BuildUpserts(ctx, nodes, rootEntity);
            }
            catch (Exception ex)
            {
                // Log error (this could be to a logger)
                Console.WriteLine($"Error compiling query: {ex.Message}");
                throw new InvalidOperationException("Failed to compile SQL query", ex);
            }
        }
    }
}