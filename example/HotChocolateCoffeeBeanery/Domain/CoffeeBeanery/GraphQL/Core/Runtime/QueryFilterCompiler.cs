using CoffeeBeanery.GraphQL.Core.Contracts;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

internal static class QueryFilterCompiler
{
    public static List<QueryFilter> Compile(ISyntaxNode whereNode)
    {
        var filters = new List<QueryFilter>();

        if (whereNode == null)
            return filters;

        Extract(whereNode, filters);

        return filters;
    }

    private static void Extract(ISyntaxNode node, List<QueryFilter> output)
    {
        foreach (var child in node.GetNodes())
        {
            var grandChildren = child.GetNodes().ToList();

            // leaf node = field condition
            if (grandChildren.Count == 1)
            {
                var opNode = grandChildren[0];
                var opParts = opNode.ToString().Split(':');

                if (opParts.Length != 2)
                    continue;

                output.Add(new QueryFilter(
                    Field: child.ToString(),
                    Operator: opParts[0].Trim(),
                    Value: opParts[1].Trim()
                ));

                continue;
            }

            // recurse into nested objects (AND/OR etc)
            Extract(child, output);
        }
    }
}