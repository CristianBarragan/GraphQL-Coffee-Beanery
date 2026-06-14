// MutationValueExtractor.cs
using CoffeeBeanery.GraphQL.Core.Runtime;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class MutationValueExtractor
{
    public static Dictionary<string, Dictionary<string, object?>> Extract(
        GraphIL     graph,
        ISyntaxNode wrapperArgValue)
    {
        var result = new Dictionary<string, Dictionary<string, object?>>(
            StringComparer.OrdinalIgnoreCase);

        WalkNode(wrapperArgValue, graph, result, depth: 0);

        return result;
    }

    private static void WalkNode(
        ISyntaxNode                                      node,
        GraphIL                                          graph,
        Dictionary<string, Dictionary<string, object?>> result,
        int                                              depth)
    {
        if (node is not ObjectValueNode objNode)
            return;

        foreach (var field in objNode.Fields)
        {
            var fieldName = field.Name.Value;

            if (field.Value is ObjectValueNode childObj)
            {
                // Exact match first, then fuzzy prefix match
                var resolvedAliases = ResolveGraphAliases(fieldName, graph);

                foreach (var resolvedAlias in resolvedAliases)
                {
                    var bag = ExtractScalars(childObj, graph, resolvedAlias);
                    if (bag.Count > 0)
                    {
                        if (!result.TryGetValue(resolvedAlias, out var existing))
                            result[resolvedAlias] = bag;
                        else
                            foreach (var kv in bag)
                                existing[kv.Key] = kv.Value;
                    }
                }

                // Always recurse
                WalkNode(childObj, graph, result, depth + 1);
            }
            else if (field.Value is ListValueNode listNode)
            {
                foreach (var item in listNode.Items)
                    WalkNode(item, graph, result, depth + 1);
            }
        }
    }

    // Resolves a GraphQL field name to one or more graph node aliases.
    // e.g. "innerCustomer" → ["InnerCustomerCustomer"]
    //      "outerCustomer" → ["OuterCustomerCustomer"]
    //      "graphModel"    → ["CustomerCustomerEdge"]  (via GraphMap)
    //      "customerCustomerRelationship" → ["CustomerCustomerRelationship"]
    private static List<string> ResolveGraphAliases(
        string  fieldName,
        GraphIL graph)
    {
        var matches = new List<string>();

        foreach (var alias in graph.Nodes.Keys)
        {
            // 1. Exact match (case-insensitive)
            if (string.Equals(alias, fieldName, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(alias);
                continue;
            }

            // 2. The alias contains the fieldName as a suffix segment
            //    "InnerCustomerCustomer".EndsWith("Customer") but we want
            //    "innerCustomer" to match "InnerCustomerCustomer" specifically.
            //    Strategy: alias starts with fieldName (ignoring case) OR
            //    alias ends with the model part of fieldName.
            //    e.g. fieldName="innerCustomer" → split by camel → ["inner","Customer"]
            //         alias="InnerCustomerCustomer" → starts with "InnerCustomer"
            var fieldUpper = ToPascalCase(fieldName);

            if (alias.StartsWith(fieldUpper, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(alias);
                continue;
            }

            // 3. Alias ends with the fieldName (e.g. "CustomerCustomerRelationship"
            //    ends with "CustomerCustomerRelationship")
            if (alias.EndsWith(fieldUpper, StringComparison.OrdinalIgnoreCase))
            {
                matches.Add(alias);
                continue;
            }

            // 4. GraphMap alias match — "graphModel" maps to the node
            //    that has a GraphMap (e.g. CustomerCustomerEdge)
            if (string.Equals(fieldName, "graphModel",
                    StringComparison.OrdinalIgnoreCase) &&
                graph.Nodes[alias].GraphMap != null)
            {
                matches.Add(alias);
            }
        }

        return matches.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static string ToPascalCase(string input)
    {
        if (string.IsNullOrEmpty(input)) return input;
        return char.ToUpperInvariant(input[0]) + input[1..];
    }

    private static Dictionary<string, object?> ExtractScalars(
        ObjectValueNode objNode,
        GraphIL         graph,
        string          nodeAlias)
    {
        var result = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        graph.Nodes.TryGetValue(nodeAlias, out var graphNode);

        var enumMaps = graphNode?.Fields
            .Where(f => f.Enumerations?.Count > 0)
            .ToDictionary(
                f => f.SourceName,
                f => f.Enumerations,
                StringComparer.OrdinalIgnoreCase)
            ?? default;

        foreach (var field in objNode.Fields)
        {
            if (field.Value is ObjectValueNode)
                continue;

            var fieldName = field.Name.Value;
            object? resolvedValue;

            if (field.Value is EnumValueNode enumNode)
            {
                var rawEnumString = enumNode.Value;

                if (enumMaps.TryGetValue(fieldName, out var enumerations))
                {
                    var matched = enumerations.FirstOrDefault(e =>
                        string.Equals(e.Key, rawEnumString,
                            StringComparison.OrdinalIgnoreCase));

                    resolvedValue = !string.IsNullOrEmpty(matched.Key)
                        ? matched.Value
                        : rawEnumString;
                }
                else
                {
                    resolvedValue = rawEnumString;
                }
            }
            else
            {
                resolvedValue = field.Value switch
                {
                    StringValueNode  s => s.Value,
                    IntValueNode     i => i.ToInt32(),
                    FloatValueNode   f => f.ToDouble(),
                    BooleanValueNode b => b.Value,
                    NullValueNode    _ => null,
                    _                  => field.Value.ToString()?.Trim('"')
                };
            }

            result[fieldName] = resolvedValue;
        }

        return result;
    }
}