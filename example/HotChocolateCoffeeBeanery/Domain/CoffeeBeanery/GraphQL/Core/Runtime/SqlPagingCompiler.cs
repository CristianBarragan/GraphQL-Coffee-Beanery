using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

internal static class SqlPagingCompiler
{
    public static void ExtractPagination(
        SqlCompilationContext ctx,
        FieldNode rootSelection)
    {
        var args = rootSelection.Arguments;

        var hasAny = false;

        foreach (var argument in args.Where(a => !a.Name.Value.Matches("where")))
        {
            var value = argument.Value?.Value?.ToString();

            switch (argument.Name.ToString())
            {
                case "first":
                    ctx.Pagination.First = ParseInt(value) ?? 0;
                    hasAny = true;
                    break;

                case "last":
                    ctx.Pagination.Last = ParseInt(value) ?? 0;
                    hasAny = true;
                    break;

                case "before":
                    ctx.Pagination.Before = value ?? "";
                    hasAny = true;
                    break;

                case "after":
                    ctx.Pagination.After = value ?? "";
                    hasAny = true;
                    break;
            }
        }

        if (hasAny)
        {
            ctx.HasPagination = true;
            ctx.Pagination.RequiresTotalCount =
                ctx.Pagination.First > 0 || ctx.Pagination.Last > 0;
        }
    }

    private static int? ParseInt(string? value)
        => int.TryParse(value, out var v) ? v : null;
}