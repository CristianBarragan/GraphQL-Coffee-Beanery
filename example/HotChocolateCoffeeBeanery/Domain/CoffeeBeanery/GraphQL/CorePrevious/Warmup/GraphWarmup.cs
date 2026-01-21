using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Compiler;

namespace CoffeeBeanery.GraphQL.Core.Warmup
{
    public static class GraphWarmup
    {
        public static void Init(Assembly modelAssembly, Assembly entityAssembly)
        {
            MappingScanner.ScanAndRegister(modelAssembly, entityAssembly);
            // Invokes mapping registration + SQL node infrastructure
            // SqlNodeBuilder.BuildFromModel<Customer>(modelAssembly, entityAssembly);
        }
    }
}