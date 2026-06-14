using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Sql;

public static class SqlNodeRegistry
{
    public static readonly Dictionary<string, Type> ModelTypes = new(StringComparer.OrdinalIgnoreCase);
    public static readonly Dictionary<string, Type> EntityTypes = new(StringComparer.OrdinalIgnoreCase);

    public static readonly List<string> ModelNames = new();
    public static readonly List<string> EntityNames = new();

    public static void Register(NodeMap map)
    {
        if (!ModelNames.Contains(map.ModelType.Name))
            ModelNames.Add(map.ModelType.Name);

        if (map.EntityType != null && !EntityNames.Contains(map.EntityType.Name))
            EntityNames.Add(map.EntityType.Name);

        ModelTypes.TryAdd(map.ModelType.Name, map.ModelType);

        if (map.EntityType != null)
            EntityTypes.TryAdd(map.EntityType.Name, map.EntityType);
    }
}