using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Mapping;

public static class GraphWarmup
{
    private static bool _initialized;

    public static void Init(Assembly assembly)
    {
        if (_initialized) return;
        _initialized = true;

        foreach (var type in assembly.GetTypes())
        {
            if (typeof(IMappingRegistration).IsAssignableFrom(type)
                && !type.IsAbstract)
            {
                var typeMapping = ((IMappingRegistration)Activator.CreateInstance(type));
                var mappings = typeMapping.Register();
                MappingWarmup.Warmup(mappings);
            }
        }
    }
}

public static class MappingWarmup
{
    public static void Warmup(IReadOnlyDictionary<string, NodeMap> mapping)
    {
        foreach (var map in mapping)
        {
            WarmupMap(map.Value);
        }
    }

    private static void WarmupMap(NodeMap map)
    {
        foreach (var field in map.FieldMaps)
        {
            if (map.EntityType != null && map.ModelType != null)
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
}