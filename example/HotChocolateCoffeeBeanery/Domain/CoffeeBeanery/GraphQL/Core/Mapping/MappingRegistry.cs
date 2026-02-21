using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using System.Collections.Generic;

public static class MappingRegistry
{
    // A dictionary to store all the model mappings by model name
    public static Dictionary<string, NodeMap> Registry { get; } = new();

    // Registers a model's mapping to the registry and ensures its presence in EntityTrees
    public static void Register(string modelName, NodeMap map)
    {
        // Ensure we're populating SqlNodeRegistry.EntityTrees if not already present
        if (!SqlNodeRegistry.EntityTrees.ContainsKey(modelName) && map.UpsertKeys.Count > 0)
        {
            SqlNodeRegistry.EntityTrees[modelName] = new NodeTree
            {
                Name = modelName,
                Schema = map.Schema,
                Children = map.Children
            };
        }

        // Register the model mapping in the registry
        Registry[modelName] = map;
    }

    // Retrieves a specific model map by its name
    public static NodeMap Get(string modelName)
    {
        return Registry[modelName];
    }

    // Retrieves all model mappings as a read-only dictionary
    public static IReadOnlyDictionary<string, NodeMap> GetAll()
    {
        return Registry;
    }
}