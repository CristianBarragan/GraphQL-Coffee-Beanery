using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SqlPaginationBuilder
    {
        public static Pagination BuildPagination(
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
                            ctx.SqlStructure!.Pagination.First = f;
                            ctx.SqlStructure!.HasPagination = true;
                        }
                        break;
                    case "after":
                        ctx.SqlStructure!.Pagination.After = arg.Value.ToString();
                        ctx.SqlStructure!.HasPagination = true;
                        break;
                }
            }
            
            return ctx.SqlStructure!.Pagination;
        }
    }
}