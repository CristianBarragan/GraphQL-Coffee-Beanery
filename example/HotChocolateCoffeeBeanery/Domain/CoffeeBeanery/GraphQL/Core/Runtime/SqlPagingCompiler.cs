using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlPagingCompiler
    {
        public static void Compile(SqlCompilationContext ctx, ISelection rootSelection)
        {
            foreach (var arg in rootSelection.SyntaxNode.Arguments)
            {
                switch (arg.Name.Value)
                {
                    case "first":
                        if (int.TryParse(arg.Value?.ToString(), out var f))
                        {
                            ctx.Pagination.First = f;
                            ctx.Pagination.PageSize = f;
                        }
                        break;
                    case "last":
                        if (int.TryParse(arg.Value?.ToString(), out var l))
                        {
                            ctx.Pagination.Last = l;
                            ctx.Pagination.PageSize = l;
                        }
                        break;
                }
            }
        }
    }
}