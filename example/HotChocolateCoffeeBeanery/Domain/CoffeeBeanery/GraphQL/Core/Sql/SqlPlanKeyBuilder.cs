using System.Text;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Sql;

public static class SqlPlanKeyBuilder
{
    public static string Build(NodeTree rootTree, ISyntaxNode rootSelection)
    {
        var sb = new StringBuilder();

        sb.Append(rootTree.Name);
        sb.Append("|");

        AppendNode(rootSelection, sb);

        return sb.ToString();
    }

    private static void AppendNode(ISyntaxNode node, StringBuilder sb)
    {
        if (node == null) return;

        var children = node.GetNodes()?.ToList();
        if (children == null || children.Count == 0)
        {
            sb.Append(node.ToString());
            sb.Append(",");
            return;
        }

        foreach (var child in children)
        {
            AppendNode(child, sb);
        }
    }
}