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

        var rootMap = ResolveNodeMap(rootAlias, rootAlias);
        if (rootMap == null)
            throw new InvalidOperationException(
                $"[NodeTreeIterator] No NodeMap registered for root alias '{rootAlias}'. " +
                $"Registered keys: [{string.Join(", ", MappingRegistry.Registry.Keys)}]");

        return IterateTree(
            nodeTrees, rootMap, rootAlias, string.Empty, rootAlias,
            nodeIds, visitedAliases, visitedMaps, ref counter)!;
    }

    private static NodeTree? IterateTree(
        Dictionary<string, NodeTree> nodeTrees,
        NodeMap nodeMap,
        string currentAlias,
        string parentAlias,
        string rootAlias,          // ADDED — carries the root prefix (e.g. "InnerCustomer")
        List<KeyValuePair<string, int>> nodeIds,
        HashSet<string> visitedAliases,
        HashSet<NodeMap> visitedMaps,
        ref int counter)
    {
        if (visitedAliases.Contains(currentAlias)) return null;
        if (visitedMaps.Contains(nodeMap)) return null;

        visitedAliases.Add(currentAlias);
        visitedMaps.Add(nodeMap);

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

        var allChildren = Enumerable.Empty<LinkKey>();

        if (nodeMap.EntityChildren != null)
            allChildren = allChildren.Concat(nodeMap.EntityChildren);

        if (nodeMap.EntityRelatedChildren != null)
            allChildren = allChildren.Concat(nodeMap.EntityRelatedChildren);

        foreach (var childLink in allChildren)
        {
            // FIXED: AliasTo carries the prefixed key set via A(nameof(T>() in mappings
            // fall back to To only if AliasTo is absent (legacy LinkKeys)
            var childAlias = !string.IsNullOrWhiteSpace(childLink.AliasTo)
                ? childLink.AliasTo
                : ResolveAliasWithPrefix(childLink.To, rootAlias);

            if (string.IsNullOrWhiteSpace(childAlias)) continue;
            if (visitedAliases.Contains(childAlias)) continue;

            if (!MappingRegistry.Registry.ContainsKey(childAlias))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(
                    $"[ERROR] NodeMap not found for child alias '{childAlias}' " +
                    $"declared in EntityChildren/EntityRelatedChildren of '{currentAlias}'. " +
                    $"Registered keys: [{string.Join(", ", MappingRegistry.Registry.Keys)}]");
                Console.ResetColor();
                continue;
            }

            var childMap = MappingRegistry.Registry[childAlias];
            if (visitedMaps.Contains(childMap)) continue;

            var childNode = IterateTree(
                nodeTrees, childMap, childAlias, currentAlias, rootAlias,
                nodeIds, visitedAliases, visitedMaps, ref counter);

            if (childNode != null && !node.Children.Any(c => c.To == childAlias))
                node.Children.Add(new LinkKey
                {
                    From       = currentAlias,
                    FromColumn = childLink.FromColumn,
                    To         = childAlias,
                    AliasTo    = childAlias,
                    ToColumn   = childLink.ToColumn
                });
        }

        nodeTrees[currentAlias] = node;
        return node;
    }

    /// <summary>
    /// Resolves a short child alias to its prefixed registry key using the root prefix.
    /// Resolution order:
    /// 1. Exact match                          — "InnerCustomerCustomer" → found directly
    /// 2. Root prefix + child alias            — "InnerCustomer" + "Customer" → "InnerCustomerCustomer"
    /// 3. Root prefix + entity type name       — handles "InnerCustomer" → "InnerCustomerCustomer"
    ///    where childAlias == entity type name
    /// 4. Any registry key ending with alias   — broadest fallback
    /// </summary>
    private static string? ResolveAliasWithPrefix(string childAlias, string rootAlias)
    {
        // 1. Exact match
        if (MappingRegistry.Registry.ContainsKey(childAlias))
            return childAlias;

        // 2. Root prefix + child alias directly
        // e.g. rootAlias="InnerCustomer", childAlias="ContactPoint"
        //   → "InnerCustomerContactPoint"
        var prefixed = $"{rootAlias}{childAlias}";
        if (MappingRegistry.Registry.ContainsKey(prefixed))
            return prefixed;

        // 3. Root prefix + just the entity type portion of childAlias
        // e.g. rootAlias="InnerCustomer", childAlias="InnerCustomer" (self-referential)
        //   → strip common prefix and try again
        if (childAlias.StartsWith(rootAlias, StringComparison.OrdinalIgnoreCase))
        {
            var stripped = childAlias.Substring(rootAlias.Length);
            var strippedPrefixed = $"{rootAlias}{stripped}";
            if (MappingRegistry.Registry.ContainsKey(strippedPrefixed))
                return strippedPrefixed;
        }

        // 4. Broadest fallback — any key ending with the child alias,
        // preferring keys that start with the root prefix
        var candidates = MappingRegistry.Registry.Keys
            .Where(k => k.EndsWith(childAlias, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return candidates.FirstOrDefault(k =>
                   k.StartsWith(rootAlias, StringComparison.OrdinalIgnoreCase))
               ?? candidates.FirstOrDefault();
    }

    private static NodeMap? ResolveNodeMap(string alias, string rootAlias)
    {
        // Try exact first, then prefix-aware resolution
        if (MappingRegistry.Registry.TryGetValue(alias, out var map))
            return map;

        var resolved = ResolveAliasWithPrefix(alias, rootAlias);
        return resolved != null && MappingRegistry.Registry.TryGetValue(resolved, out var resolvedMap)
            ? resolvedMap
            : null;
    }
}