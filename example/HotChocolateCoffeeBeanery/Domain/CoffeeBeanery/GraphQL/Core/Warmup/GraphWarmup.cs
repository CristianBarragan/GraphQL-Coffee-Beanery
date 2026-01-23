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
                ((IMappingRegistration)Activator.CreateInstance(type)).Register();
            }
        }
    }
}