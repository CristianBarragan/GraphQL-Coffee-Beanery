using System.Linq.Expressions;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Helper;
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Warmup;

public static class MappingWarmup
{
    public static void Warmup(IReadOnlyDictionary<string, NodeMap> mapping)
    {
        var nodeTrees = new Dictionary<string, NodeTree>();
        var nodeIds = new List<KeyValuePair<string, int>>();

        foreach (var map in mapping.Values)
        {
            if (map.ModelType == null || map.EntityType == null)
                continue;

            // Dynamically create an instance of the model type
            object? modelInstance = null;
            try
            {
                modelInstance = Activator.CreateInstance(map.ModelType);
            }
            catch
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARNING] Could not instantiate {map.ModelType.FullName}");
                Console.ResetColor();
                continue;
            }

            // Generate tree for this node
            NodeTreeIterator.GenerateTree(
                nodeTrees,
                modelInstance,
                map.ModelType.Name,
                nodeIds,
                isModel: true
            );

            // Warmup properties for mapping
            WarmupMap(map);

            // Compile the mapper
            BulkMapper.Compile(map);
        }
    }

    private static void WarmupMap(NodeMap map)
    {
        foreach (var field in map.FieldMaps.Where(a => a.DestinationName != "Id"))
        {
            var modelProp = map.ModelType.GetProperty(field.SourceName);
            var entityProp = map.EntityType.GetProperty(field.DestinationName);

            if (modelProp != null)
                map.ModelProperties[field.SourceName] = modelProp;

            if (entityProp != null)
                map.EntityProperties[field.DestinationName] = entityProp;
        }
    }
}