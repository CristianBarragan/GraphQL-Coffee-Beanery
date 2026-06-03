using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Helper;
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Warmup;

public static class MappingWarmup
{
    public static void Warmup(IReadOnlyDictionary<string, NodeMap> mapping)
    {
        var nodeTrees = new Dictionary<string, NodeTree>();
        var nodeIds   = new List<KeyValuePair<string, int>>();
        var counter   = 0;

        foreach (var (_, map) in mapping)
        {
            if (map.ModelType == null || map.EntityType == null)
                continue;

            WarmupMap(map);
            BulkMapper.Compile(map);
        }

        foreach (var (registeredKey, map) in mapping)
        {
            if (map.ModelType == null || map.EntityType == null)
            {
                Console.WriteLine($"[SKIP] {registeredKey} — null ModelType or EntityType");
                continue;
            }

            if (map.EntityParents.Count > 0 || map.EntityRelatedParents.Count > 0)
            {
                Console.WriteLine($"[SKIP] {registeredKey} — has EntityParents({map.EntityParents.Count}) or EntityRelatedParents({map.EntityRelatedParents.Count})");
                continue;
            }

            Console.WriteLine($"[ROOT] {registeredKey} — will generate tree");

            var rootAlias = registeredKey;
            
            map.Alias = registeredKey;

            if (nodeTrees.ContainsKey(rootAlias)) continue;

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

            try
            {
                Console.WriteLine($"[ROOT] {registeredKey} — EntityChildren: [{string.Join(", ", map.EntityChildren.Select(c => c.To))}]");
                Console.WriteLine($"[ROOT] {registeredKey} — EntityRelatedChildren: [{string.Join(", ", map.EntityRelatedChildren.Select(c => c.To))}]");
                
                NodeTreeIterator.GenerateTree(
                    nodeTrees,
                    modelInstance,
                    rootAlias,
                    nodeIds,
                    isModel: true,
                    ref counter
                );
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARNING] GenerateTree failed for alias '{rootAlias}': {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    private static void WarmupMap(NodeMap map)
    {
        foreach (var field in map.FieldMaps.Where(a => a.DestinationName != "Id"))
        {
            var modelProp  = map.ModelType.GetProperty(field.SourceName);
            var entityProp = map.EntityType.GetProperty(field.DestinationName);

            if (modelProp != null)
                map.ModelProperties[field.SourceName] = modelProp;

            if (entityProp != null)
                map.EntityProperties[field.DestinationName] = entityProp;
        }
    }
}