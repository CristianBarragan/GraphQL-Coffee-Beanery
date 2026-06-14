using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public sealed class GraphQLFieldExtractor
{
    public void Extract(
        ISyntaxNode node,
        NodeTree tree,
        ColumnDependencyTracker tracker)
    {
        if (node == null)
            return;

        var children = node.GetNodes()?.ToList();

        if (children == null || children.Count == 0)
        {
            var fieldName = node.ToString()?.Trim();

            if (!string.IsNullOrWhiteSpace(fieldName))
                tracker.Add(tree.Alias, fieldName);

            return;
        }

        foreach (var child in children)
            Extract(child, tree, tracker);
    }
}

public static class SplitOnBuilder
{
    public static Dictionary<string, Type> Build(
        QueryPlan plan,
        Dictionary<string, NodeTree> entityTrees)
    {
        var result = new Dictionary<string, Type>(
            StringComparer.OrdinalIgnoreCase);

        foreach (var node in plan.Nodes.Values)
        {
            if (entityTrees.TryGetValue(node.Alias, out var tree))
                result[node.Alias] = tree.EntityType;
        }

        return result;
    }
}