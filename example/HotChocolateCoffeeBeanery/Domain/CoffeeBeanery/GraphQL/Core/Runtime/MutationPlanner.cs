using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.Runtime;

namespace HotChocolateCoffeeBeanery.GraphQL.Core.Runtime;

public class MutationPlanner {
    // MutationPlanner.cs — rootAlias removed
// MutationPlanner.cs — only include nodes that have matching input values
public static MutationPlan Build(
    GraphIL                                          graph,
    Dictionary<string, Dictionary<string, object?>> mutationValues)
{
    var nodes = new Dictionary<string, MutationNode>(StringComparer.OrdinalIgnoreCase);

    // Only walk aliases that actually have input — do NOT walk graph edges
    // to discover extra nodes, that's what pulls in empty CBR/ContactPoint
    foreach (var (alias, input) in mutationValues)
    {
        if (!graph.Nodes.TryGetValue(alias, out var graphNode))
            continue;

        if (graphNode.EntityType == null)
            continue;

        var identity = new List<MutationField>();
        var data     = new List<MutationField>();

        foreach (var key in graphNode.UpsertKeys)
        {
            if (input.TryGetValue(key, out var val))
                identity.Add(new MutationField(key, key, val));
        }

        foreach (var f in graphNode.Fields)
        {
            if (input.TryGetValue(f.SourceName, out var val) &&
                !graphNode.UpsertKeys.Contains(f.SourceName))
            {
                data.Add(new MutationField(f.SourceName, f.DestinationName, val));
            }
        }

        // Skip nodes where we found no identity key — can't upsert without it
        if (identity.Count == 0)
            continue;

        nodes[alias] = new MutationNode(
            alias,
            graphNode.EntityType.Name,
            "UPSERT",
            identity,
            data,
            graphNode.EntityType);
    }

    if (nodes.Count == 0)
        throw new InvalidOperationException(
            $"Mutation produced no fields. " +
            $"Input aliases: [{string.Join(", ", mutationValues.Keys)}]. " +
            $"Graph aliases: [{string.Join(", ", graph.Nodes.Keys)}].");

    var rootNode =
        nodes.Values.FirstOrDefault(n => n.IdentityFields.Count > 0) ??
        nodes.Values.First();

    return new MutationPlan(nodes, new List<MutationStatement>(), rootNode);
}
}

