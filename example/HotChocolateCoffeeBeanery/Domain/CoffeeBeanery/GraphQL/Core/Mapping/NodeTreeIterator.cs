using System;
using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

public static class NodeTreeIterator
{
    public static NodeTree GenerateTree<M>(
        Dictionary<string, NodeTree> nodeTrees,
        M rootInstance,
        string rootAlias,
        List<KeyValuePair<string, int>> nodeIds,
        bool isModel,
        ref int counter)
        where M : class
    {
        var visitedAliases = new HashSet<string>();
        var visitedMaps    = new HashSet<NodeMap>(ReferenceEqualityComparer.Instance);

        var rootMap = ResolveNodeMap(rootAlias);
        if (rootMap == null)
            throw new InvalidOperationException(
                $"[NodeTreeIterator] No NodeMap registered for root alias '{rootAlias}'. " +
                $"Registered keys: [{string.Join(", ", MappingRegistry.Registry.Keys)}]");

        return IterateTree(
            nodeTrees, rootMap, rootAlias, string.Empty,
            nodeIds, visitedAliases, visitedMaps, ref counter)!;
    }

    private static NodeTree? IterateTree(
        Dictionary<string, NodeTree> nodeTrees,
        NodeMap nodeMap,
        string currentAlias,
        string parentAlias,
        List<KeyValuePair<string, int>> nodeIds,
        HashSet<string> visitedAliases,
        HashSet<NodeMap> visitedMaps,
        ref int counter)
    {
        if (visitedAliases.Contains(currentAlias)) return null;
        if (visitedMaps.Contains(nodeMap)) return null;

        visitedAliases.Add(currentAlias);
        visitedMaps.Add(nodeMap);

        // Assign ID immediately — depth-first pre-order
        var id = ++counter;
        nodeIds.Add(new KeyValuePair<string, int>(currentAlias, id));

        nodeMap.Id    = id;
        nodeMap.Alias = currentAlias;

        var node = new NodeTree
        {
            Id       = id,
            Name     = currentAlias,
            Children = new List<LinkKey>(),
            Mapping  = nodeMap.FieldMaps,
            Schema   = nodeMap.Schema
        };

        // Combine EntityChildren and EntityRelatedChildren — both are treated
        // identically as child links for tree traversal and SQL JOIN generation
        var allChildren = Enumerable.Empty<LinkKey>();

        if (nodeMap.EntityChildren != null)
            allChildren = allChildren.Concat(nodeMap.EntityChildren);

        if (nodeMap.EntityRelatedChildren != null)
            allChildren = allChildren.Concat(nodeMap.EntityRelatedChildren);

        foreach (var childLink in allChildren)
        {
            // childLink.To MUST exactly match the registered alias key
            var childAlias = childLink.To;

            if (visitedAliases.Contains(childAlias)) continue;

            var childMap = ResolveNodeMap(childAlias);

            if (childMap == null)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    $"[ERROR] NodeMap not found for child alias '{childAlias}' " +
                    $"declared in EntityChildren/EntityRelatedChildren of '{currentAlias}'. " +
                    $"Registered keys: [{string.Join(", ", MappingRegistry.Registry.Keys)}]");
                Console.ResetColor();
                continue;
            }

            if (visitedMaps.Contains(childMap)) continue;

            var childNode = IterateTree(
                nodeTrees, childMap, childAlias, currentAlias,
                nodeIds, visitedAliases, visitedMaps, ref counter);

            if (childNode != null)
            {
                if (!node.Children.Any(c => c.To == childAlias))
                    node.Children.Add(new LinkKey
                    {
                        From       = currentAlias,
                        FromColumn = childLink.FromColumn,
                        To         = childAlias,
                        ToColumn   = childLink.ToColumn
                    });
            }
        }

        nodeTrees[currentAlias] = node;
        return node;
    }

    private static NodeMap? ResolveNodeMap(string alias) =>
        MappingRegistry.Registry.TryGetValue(alias, out var map) ? map : null;
}