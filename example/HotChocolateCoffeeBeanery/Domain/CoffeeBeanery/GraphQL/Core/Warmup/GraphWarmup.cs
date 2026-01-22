using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Warmup
{
    public static class GraphWarmup
    {
        public static void Init(Assembly modelAssembly)
        {
            foreach (var type in modelAssembly.GetTypes())
            {
                if (typeof(IMappingRegistration).IsAssignableFrom(type) && !type.IsAbstract)
                {
                    var instance = (IMappingRegistration)Activator.CreateInstance(type);
                    instance.Register();
                }
            }

        }
    }
}