using CoffeeBeanery.GraphQL.Core.Contracts;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

internal static class SqlOrderCompiler
{
    public static List<OrderInstruction> Compile(
        ISyntaxNode orderNode)
    {
        var result = new List<OrderInstruction>();

        Extract(orderNode, result);

        return result;
    }

    private static void Extract(
        ISyntaxNode node,
        List<OrderInstruction> output)
    {
        foreach (var child in node.GetNodes())
        {
            var text = child.ToString();

            // ignore nested selections
            if (text.Contains("{"))
            {
                Extract(child, output);
                continue;
            }

            // parse "field: ASC|DESC"
            if (text.Contains(":"))
            {
                var parts = text.Split(':', StringSplitOptions.TrimEntries);

                if (parts.Length != 2)
                    continue;

                var field = parts[0];
                var direction = parts[1];

                output.Add(new OrderInstruction(
                    Alias: null,
                    Field: field,
                    direction.Contains("DESC", StringComparison.OrdinalIgnoreCase)
                        ? SortDirection.Desc
                        : SortDirection.Asc));
            }

            Extract(child, output);
        }
    }
}