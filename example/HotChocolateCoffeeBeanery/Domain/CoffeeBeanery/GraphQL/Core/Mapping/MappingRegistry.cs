using CoffeeBeanery.GraphQL.Core.Mapping;

public static class MappingRegistry
{
    public static Dictionary<string, EntityMap> Registry { get; } = new();

    public static void Register(string modelName, EntityMap map)
    {
        Registry[modelName] = map;
    }

    public static EntityMap Get(string modelName)
    {
        return Registry[modelName];
    }
    
    public static IReadOnlyDictionary<string, EntityMap> GetAll()
    {
        return Registry;
    }
}