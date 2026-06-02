using System;
using System.Linq;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Warmup;
using CoffeeBeanery.GraphQL.Helper;

public static class GraphWarmup
{
    private static bool _initialized;

    public static void Init<TSet, TEnum, T2Enum>(
        this IServiceCollection services,
        Assembly assembly)
        where TSet : IMappingSet<TEnum, T2Enum>
        where TEnum  : Enum
        where T2Enum : Enum
    {
        if (_initialized) return;
        _initialized = true;

        var sets = assembly.GetTypes()
            .Where(t => typeof(TSet).IsAssignableFrom(t)
                        && !t.IsInterface
                        && !t.IsAbstract)
            .Select(t => (TSet)Activator.CreateInstance(t)!)
            .ToList(); // materialise once — avoids re-instantiating on each iteration
        
        var enum1Values = Enum.GetValues(typeof(TEnum)).Cast<TEnum>().ToList();
        var enum2Values = Enum.GetValues(typeof(T2Enum)).Cast<T2Enum>().ToList();

        foreach (var type in enum1Values)
        {
            foreach (var type2 in enum2Values)
            {
                foreach (var set in sets)
                {
                    if ($"{type2.ToString()}MappingSet".Matches(set.GetType().Name) ||
                        ($"{type.ToString()}MappingSet".Matches(set.GetType().Name) && type2.ToString() == type.ToString()))
                    {
                        set.Register(type, type2);
                    }
                }
            }
        }

        MappingWarmup.Warmup(MappingRegistry.Registry);

        services.AddSingleton<IMapper>(
            new Mapper(MappingRegistry.Registry));
    }
}