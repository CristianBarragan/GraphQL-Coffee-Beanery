using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Warmup;

public static class GraphWarmup
{
    private static bool _initialized;

    public static void Init(this IServiceCollection services, Assembly assembly)
    {
        if (_initialized) return;
        _initialized = true;

        var mappings = new Dictionary<string, NodeMap>();

        foreach (var type in assembly.GetTypes())
        {
            if (typeof(IMappingRegistration).IsAssignableFrom(type) && !type.IsAbstract)
            {
                var typeMapping = (IMappingRegistration)Activator.CreateInstance(type);
                typeMapping.Register(mappings); // ✅ Pass dictionary, no return
            }
        }

        // 2️⃣ Warmup all mappings
        MappingWarmup.Warmup(mappings);

        // 3️⃣ Register Mapper service
        services.AddSingleton<IMapper>(new Mapper(mappings));
    }
}