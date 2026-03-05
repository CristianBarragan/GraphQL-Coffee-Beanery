using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using System.Collections.Generic;

public static class MappingRegistry
{
    public static Dictionary<string, NodeMap> Registry { get; } = new();
    
    public static NodeMap Register(Type modelType, Type entityType, NodeMap map)
    {
        if (!SqlNodeRegistry.EntityTrees.ContainsKey(modelType.Name) && map.UpsertKeys.Count > 0)
        {
            SqlNodeRegistry.EntityTrees[modelType.Name] = new NodeTree
            {
                Name = modelType.Name,
                Schema = map.Schema,
                Children = map.Children
            };
        }
        
        map.EntityType = entityType;
        map.ModelType = modelType;
        
        Registry[modelType.Name] = map;

        return map;
    }

    public static NodeMap Get(string modelName)
    {
        return Registry[modelName];
    }

    public static IReadOnlyDictionary<string, NodeMap> GetAll()
    {
        return Registry;
    }
}