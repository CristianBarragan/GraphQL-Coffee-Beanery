using System.Reflection;

namespace CoffeeBeanery.GraphQL.Core.Warmup;

public class RuntimeWarmup
{
    public Dictionary<(Type, string), PropertyInfo> PropertyCache { get; } = new();
    public Dictionary<Type, Func<object[], object>> Hydrators { get; } = new();
}