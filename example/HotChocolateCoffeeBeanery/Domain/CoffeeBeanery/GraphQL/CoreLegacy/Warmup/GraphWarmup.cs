using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Configuration;
using CoffeeBeanery.GraphQL.Core.Builder;

namespace CoffeeBeanery.GraphQL.Core.Warmup
{
    public static class GraphWarmup
    {
        public static void Init(Assembly modelAssembly, Assembly entityAssembly)
        {
            // Invokes mapping registration + SQL node infrastructure
            SqlNodeBuilder.BuildFromModels(modelAssembly, entityAssembly);
        }
    }
}