using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlPagingCompiler
    {
        public static void Compile(
            SqlCompilationContext ctx,
            ISelection rootSelection)
        {
            foreach (var arg in rootSelection.SyntaxNode.Arguments)
            {
                var name = arg.Name.Value;

                switch (name)
                {
                    case "first":
                        if (int.TryParse(arg.Value?.ToString(), out var f))
                        {
                            ctx.Pagination.First = f;
                            ctx.HasPagination = true;
                        }
                        break;

                    case "last":
                        if (int.TryParse(arg.Value?.ToString(), out var l))
                        {
                            ctx.Pagination.Last = l;
                            ctx.HasPagination = true;
                        }
                        break;

                    case "after":
                        ctx.Pagination.After = arg.Value?.ToString() ?? "";
                        ctx.HasPagination = true;
                        break;

                    case "before":
                        ctx.Pagination.Before = arg.Value?.ToString() ?? "";
                        ctx.HasPagination = true;
                        break;
                }
            }
        }
    }
}