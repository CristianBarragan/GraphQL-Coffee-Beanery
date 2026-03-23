using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using System.Collections.Generic;

public static class MappingRegistry
{
    public static Dictionary<string, NodeMap> Registry { get; } = new();
    
    public static NodeMap Register(Type modelType, Type entityType, NodeMap map, string name = "")
    {
        if (string.IsNullOrEmpty(name))
        {
            if (modelType != null)
            {
                name = modelType.Name;
            }
            else if (entityType != null)
            {
                name = entityType.Name;
            }
        }
        
        if (entityType != null && !SqlNodeRegistry.EntityTrees.ContainsKey(name) && map.UpsertKeys.Count > 0)
        {
            SqlNodeRegistry.EntityTrees[name] = new NodeTree
            {
                Id = map.Id,
                Name = entityType.Name,
                Alias = name,
                Schema = map.Schema,
                Children = map.Children
            };
        }
        
        map.EntityType = entityType;
        map.ModelType = modelType;
        map.Alias = name;
        
        Registry[modelType.Name] = map;

        return map;
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