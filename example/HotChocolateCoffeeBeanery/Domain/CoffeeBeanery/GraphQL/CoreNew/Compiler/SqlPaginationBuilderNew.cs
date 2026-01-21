using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.CoreNew.Compiler
{
    public static class SqlPaginationBuilderNew
    {
        public static void BuildPagination(
            SqlCompilationContext ctx,
            ISelection root)
        {
            foreach (var arg in root.SyntaxNode.Arguments)
            {
                switch (arg.Name.Value)
                {
                    case "first":
                        if (int.TryParse(arg.Value.ToString(), out var f))
                        {
                            ctx.Pagination.First = f;
                            ctx.HasPagination = true;
                        }
                        break;
                    case "after":
                        ctx.Pagination.After = arg.Value.ToString();
                        ctx.HasPagination = true;
                        break;
                }
            }
        }
    }
}
