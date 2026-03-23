using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Extension;

public static class NodeTreeIterator
{
    /// <summary>
    /// Generate the full NodeTree hierarchy from a root instance
    /// </summary>
    public static NodeTree GenerateTree<M>(
        Dictionary<string, NodeTree> nodeTrees,
        M rootInstance,
        string rootAlias,
        List<KeyValuePair<string, int>> nodeIds,
        bool isModel)
        where M : class
    {
        var visited = new HashSet<string>();
        return IterateTree(
            nodeTrees,
            rootInstance,
            rootAlias,
            parentAlias: string.Empty,
            nodeIds,
            isModel,
            visited
        )!;
    }

    /// <summary>
    /// Recursive iterator: handles reflection and registry-based children
    /// </summary>
    private static NodeTree? IterateTree<M>(
        Dictionary<string, NodeTree> nodeTrees,
        M? currentInstance,
        string currentAlias,
        string parentAlias,
        List<KeyValuePair<string, int>> nodeIds,
        bool isModel,
        HashSet<string> visited)
        where M : class
    {
        if (visited.Contains(currentAlias))
            return null; // cycle detected

        visited.Add(currentAlias);

        var currentType = currentInstance!.GetType();

        // Assign node ID and parent ID
        var id = RegisterNodeId(nodeIds, currentAlias);
        var parentId = ResolveParentId(nodeIds, parentAlias);

        var node = new NodeTree
        {
            Id = id,
            Name = currentAlias,
            ParentName = parentAlias,
            Children = new List<string>()
        };

        // Hydrate NodeMap from registry if exists
        var nodeMap = ResolveNodeMap(currentAlias);
        if (nodeMap == null)
        {
            ReportMissingMapping(currentAlias, currentType);
        }
        else
        {
            nodeMap.Id = node.Id;
            nodeMap.Alias = currentAlias;
            
            if (nodeMap != null)
            {
                node.Children = new List<string>(nodeMap.Children);
                node.Mapping = nodeMap.FieldMaps;
                node.Schema = nodeMap.Schema;
            }
        }
        
        foreach (var prop in currentType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (GraphQLFieldExtension.IsPrimitiveType(prop.PropertyType))
                continue;

            var childInstance = TryCreateChildInstance(prop);
            if (childInstance == null)
                continue;

            var childAlias = string.IsNullOrEmpty(currentAlias)
                ? prop.Name
                : $"{currentAlias}.{prop.Name}";

            var childNode = IterateTree(
                nodeTrees,
                childInstance,
                childAlias,
                currentAlias,
                nodeIds,
                isModel,
                visited
            );

            if (childNode != null)
                node.Children.Add(childAlias);
        }

        if (nodeMap?.Children != null)
        {
            foreach (var childAlias in nodeMap.Children)
            {
                if (node.Children.Contains(childAlias))
                    continue;

                var childMap = ResolveNodeMap(childAlias);
                if (childMap == null)
                {
                    ReportMissingMapping(childAlias, typeof(object));
                    continue;
                }

                // Instantiate the child from its ModelType or EntityType
                var childInstance = childMap.ModelType != null
                    ? Activator.CreateInstance(childMap.ModelType)
                    : childMap.EntityType != null
                        ? Activator.CreateInstance(childMap.EntityType)
                        : null;

                if (childInstance == null)
                    continue;

                var childNode = IterateTree(
                    nodeTrees,
                    childInstance,
                    childAlias,
                    currentAlias,
                    nodeIds,
                    isModel,
                    visited
                );

                if (childNode != null)
                    node.Children.Add(childAlias);
            }
        }

        if (nodeMap?.Children != null)
        {
            foreach (var childAlias in nodeMap.Children)
            {
                if (node.Children.Contains(childAlias))
                    continue;

                var childMap = ResolveNodeMap(childAlias);
                if (childMap == null)
                {
                    ReportMissingMapping(childAlias, typeof(object));
                    continue;
                }

                var childInstance = childMap.ModelType != null
                    ? Activator.CreateInstance(childMap.ModelType)
                    : childMap.EntityType != null
                        ? Activator.CreateInstance(childMap.EntityType)
                        : null;

                if (childInstance == null)
                    continue;

                var childNode = IterateTree(
                    nodeTrees,
                    childInstance,
                    childAlias,
                    currentAlias,
                    nodeIds,
                    isModel,
                    visited
                );

                if (childNode != null)
                    node.Children.Add(childAlias);
            }

            // Copy NodeMap children to NodeTree
            node.Children = new List<string>(nodeMap.Children);
            node.Mapping = nodeMap.FieldMaps;
            node.Schema = nodeMap.Schema;
        }

        // Save node into tree dictionary
        nodeTrees[currentAlias] = node;

        return node;
    }

    // ────────────────────────────────
    // HELPERS
    // ────────────────────────────────

    private static NodeMap? ResolveNodeMap(string alias)
    {
        if (MappingRegistry.Registry.TryGetValue(alias, out var map))
            return map;
        return null;
    }

    private static int RegisterNodeId(List<KeyValuePair<string, int>> nodeIds, string alias)
    {
        var existing = nodeIds.FirstOrDefault(x => x.Key == alias);
        if (!string.IsNullOrEmpty(existing.Key))
            return existing.Value;

        var id = nodeIds.Count + 1;
        nodeIds.Add(new KeyValuePair<string, int>(alias, id));
        return id;
    }

    private static int ResolveParentId(List<KeyValuePair<string, int>> nodeIds, string parentAlias)
    {
        if (string.IsNullOrEmpty(parentAlias))
            return 0;
        return nodeIds.FirstOrDefault(x => x.Key == parentAlias).Value;
    }

    private static object? TryCreateChildInstance(PropertyInfo property)
    {
        var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

        if (typeof(System.Collections.IList).IsAssignableFrom(type))
        {
            var itemType = type.GenericTypeArguments.FirstOrDefault();
            return itemType != null ? Activator.CreateInstance(itemType) : null;
        }

        if (type.IsClass && type != typeof(string))
            return Activator.CreateInstance(type);

        return null;
    }

    private static void ReportMissingMapping(string alias, Type type)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine($"[WARNING] Missing NodeMap for alias: '{alias}' (Type: {type.Name})");
        Console.ResetColor();
    }
}