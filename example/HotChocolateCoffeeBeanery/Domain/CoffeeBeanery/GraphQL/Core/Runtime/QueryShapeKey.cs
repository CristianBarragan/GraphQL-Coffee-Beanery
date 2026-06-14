using System.Text;
using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.GraphQL;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class QueryShapeKey
{
    public static string Build(NodeTree root)
    {
        var sb = new StringBuilder();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Walk(NodeTree node)
        {
            if (node == null || !visited.Add(node.Alias))
                return;

            sb.Append(node.Alias).Append("|");

            // deterministic field order
            foreach (var field in node.SelectedFields.OrderBy(x => x))
                sb.Append(field).Append(",");

            sb.Append("=>");

            foreach (var child in node.Children.OrderBy(c => c.Alias))
                Walk(child);

            foreach (var related in node.RelatedChildren.OrderBy(c => c.Alias))
                Walk(related);
        }

        Walk(root);
        return sb.ToString();
    }
}