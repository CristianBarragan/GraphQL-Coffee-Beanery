using System;
using System.Linq;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Warmup;

public static class GraphWarmup
{
    private static bool _initialized;

    public static void Init<TSet, TEnum>(this IServiceCollection services, Assembly assembly)
        where TSet : IMappingSet<TEnum>
        where TEnum : Enum
    {
        if (_initialized) return;
        _initialized = true;

        if (!typeof(TEnum).IsEnum)
            throw new ArgumentException($"{typeof(TEnum).Name} is not an Enum type.");

        var sets = assembly.GetTypes()
            .Where(t => typeof(TSet).IsAssignableFrom(t)
                        && !t.IsInterface
                        && !t.IsAbstract)
            .Select(t => (TSet)Activator.CreateInstance(t)!);

        foreach (TEnum type in Enum.GetValues(typeof(TEnum)))
        {
            foreach (var set in sets)
            {
                set.Register(type);
            }
        }

        MappingWarmup.Warmup(MappingRegistry.Registry);

        services.AddSingleton<IMapper>(
            new Mapper(MappingRegistry.Registry));
    }
}