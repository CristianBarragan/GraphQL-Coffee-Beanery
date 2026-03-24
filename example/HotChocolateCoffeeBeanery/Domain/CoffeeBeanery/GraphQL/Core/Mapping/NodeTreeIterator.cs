using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Extension;

public static class NodeTreeIterator
{
    public static NodeTree GenerateTree<M>(
        Dictionary<string, NodeTree> nodeTrees,
        M rootInstance,
        string rootAlias,
        List<KeyValuePair<string, int>> nodeIds,
        bool isModel)
        where M : class
    {
        var visitedAliases = new HashSet<string>();
        var visitedMaps = new HashSet<NodeMap>(ReferenceEqualityComparer.Instance);
        
        return IterateTree(nodeTrees, typeof(M), rootAlias, string.Empty, 
            nodeIds, isModel, visitedAliases, visitedMaps)!;
    }

    private static NodeTree? IterateTree(
        Dictionary<string, NodeTree> nodeTrees,
        Type? currentType,
        string currentAlias,
        string parentAlias,
        List<KeyValuePair<string, int>> nodeIds,
        bool isModel,
        HashSet<string> visitedAliases,
        HashSet<NodeMap> visitedMaps,
        NodeMap? injectedMap = null)
    {
        if (currentType == null) return null;
        if (visitedAliases.Contains(currentAlias)) return null;

        // Resolve map early so we can check map-level cycles
        var nodeMap = injectedMap ?? ResolveNodeMap(currentAlias);

        // Block if we've already fully processed this exact NodeMap instance
        // under a different alias — prevents cross-path cycles
        if (nodeMap != null && visitedMaps.Contains(nodeMap)) return null;

        visitedAliases.Add(currentAlias);
        if (nodeMap != null) visitedMaps.Add(nodeMap);

        var id = RegisterNodeId(nodeIds, currentAlias);

        var node = new NodeTree
        {
            Id = id,
            Name = currentAlias,
            ParentName = parentAlias,
            Children = new List<string>()
        };

        if (nodeMap != null)
        {
            nodeMap.Id = node.Id;
            nodeMap.Alias = currentAlias;
            node.Mapping = nodeMap.FieldMaps;
            node.Schema = nodeMap.Schema;
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARNING] Missing NodeMap for '{currentAlias}' ({currentType.Name})");
            Console.ResetColor();
        }

        // ── Path 1: CLR property reflection ──────────────────────────────────
        foreach (var prop in currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (GraphQLFieldExtension.IsPrimitiveType(prop.PropertyType)) continue;

            var childType = ResolveChildType(prop.PropertyType);
            if (childType == null) continue;

            var childAlias = string.IsNullOrEmpty(currentAlias)
                ? prop.Name
                : $"{currentAlias}.{prop.Name}";

            if (visitedAliases.Contains(childAlias)) continue;

            var childMap = ResolveNodeMap(childAlias) ?? ResolveNodeMap(prop.Name);
            if (childMap != null && visitedMaps.Contains(childMap)) continue;

            var childNode = IterateTree(nodeTrees, childType, childAlias, currentAlias,
                nodeIds, isModel, visitedAliases, visitedMaps, childMap);

            if (childNode != null) node.Children.Add(childAlias);
        }

        // ── Path 2: NodeMap declared children ────────────────────────────────
        if (nodeMap?.Children != null)
        {
            foreach (var childName in nodeMap.Children)
            {
                var childAlias = string.IsNullOrEmpty(currentAlias)
                    ? childName
                    : $"{currentAlias}.{childName}";

                if (visitedAliases.Contains(childAlias)) continue;
                if (node.Children.Contains(childAlias)) continue;

                var childMap = ResolveNodeMap(childName);
                if (childMap == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[WARNING] No NodeMap for child '{childName}' (alias '{childAlias}')");
                    Console.ResetColor();
                    continue;
                }

                // Skip if this exact map instance was already expanded elsewhere
                if (visitedMaps.Contains(childMap)) continue;

                var childType = childMap.ModelType ?? childMap.EntityType;
                if (childType == null) continue;

                var childNode = IterateTree(nodeTrees, childType, childAlias, currentAlias,
                    nodeIds, isModel, visitedAliases, visitedMaps, childMap);

                if (childNode != null) node.Children.Add(childAlias);
            }
        }

        nodeTrees[currentAlias] = node;
        return node;
    }

    // ────────────────────────────────
    // HELPERS
    // ────────────────────────────────

    private static NodeMap? ResolveNodeMap(string alias)
    {
        return MappingRegistry.Registry.TryGetValue(alias, out var map) ? map : null;
    }

    private static int RegisterNodeId(List<KeyValuePair<string, int>> nodeIds, string alias)
    {
        var existing = nodeIds.FirstOrDefault(x => x.Key == alias);
        if (!string.IsNullOrEmpty(existing.Key)) return existing.Value;

        var id = nodeIds.Count + 1;
        nodeIds.Add(new KeyValuePair<string, int>(alias, id));
        return id;
    }

    private static Type? ResolveChildType(Type propertyType)
    {
        var type = Nullable.GetUnderlyingType(propertyType) ?? propertyType;

        if (typeof(System.Collections.IList).IsAssignableFrom(type))
            return type.GenericTypeArguments.FirstOrDefault();

        if (type.IsClass && type != typeof(string))
            return type;

        return null;
    }
}