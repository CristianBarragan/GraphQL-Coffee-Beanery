namespace CoffeeBeanery.GraphQL.Core.Mapping;

public static class MappingHelper
{
    public static void RegisterNodeMap(Dictionary<string, NodeMap> mappings, NodeMap nodeMap, string? alias = null)
    {
        if (string.IsNullOrEmpty(alias))
            alias = nodeMap.ModelType?.Name ?? nodeMap.EntityType?.Name ?? "Unknown";

        if (!string.IsNullOrEmpty(alias))
        {
            mappings.TryAdd(alias, nodeMap);
        }
    }
}