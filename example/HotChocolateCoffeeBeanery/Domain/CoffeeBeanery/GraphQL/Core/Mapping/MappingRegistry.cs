using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using System.Collections.Generic;

public static class MappingRegistry
{
    public static Dictionary<string, NodeMap> Registry { get; } = new();
    private static int _idCounter = 0;
    
    public static NodeMap Register(
        Type modelType,
        Type entityType,
        NodeMap map,
        string? alias = null)
    {
        map.ModelType = modelType;
        map.EntityType = entityType;
        
        var simpleKey = alias ?? modelType.Name;
        map.Alias = simpleKey;
        Registry[simpleKey] = map;

        return map;
    }
    
    public static void BuildDottedAliases(int maxDepth = 10)
    {
        var seen = new HashSet<string>();

        for (int depth = 0; depth < maxDepth; depth++)
        {
            bool added = false;

            foreach (var (parentAlias, parentMap) in Registry.ToList())
            {
                if (seen.Contains(parentAlias)) continue;

                foreach (var childName in parentMap.Children)
                {
                    var dottedAlias = $"{parentAlias}.{childName}";
                    if (Registry.ContainsKey(dottedAlias)) continue;

                    if (!Registry.TryGetValue(childName, out var childMap)) continue;

                    Registry[dottedAlias] = childMap;
                    added = true;
                }

                seen.Add(parentAlias);
            }

            if (!added) break;
        }
    }

    public static NodeMap Get(string alias)
    {
        return Registry[alias];
    }

    public static IReadOnlyDictionary<string, NodeMap> GetAll()
    {
        return Registry;
    }
}