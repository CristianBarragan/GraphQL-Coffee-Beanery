using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.CoreNew.Compiler
{
    public static class SqlQueryCompilerNew
    {
        public static string CompileQuery<TModel>(
            ISelection rootSelection,
            string rootEntity)
            where TModel : class
        {
            var ctx = new SqlCompilationContext();

            var nodes = SqlNodeResolverNew.ResolveFromSelection<TModel>(
                rootSelection, rootEntity, false);

            SqlWhereBuilderNew.BuildWhere(ctx, rootSelection, nodes, rootEntity);
            SqlOrderByBuilderNew.BuildOrderBy(ctx, rootSelection, nodes, rootEntity);
            SqlPaginationBuilderNew.BuildPagination(ctx, rootSelection);
            return SqlSelectGeneratorNew.BuildSelect(ctx, nodes, rootEntity);
        }
    }
}
