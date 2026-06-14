using System.Reflection;

namespace CoffeeBeanery.GraphQL.Core.Warmup;

public static class ReflectionWarmup
{
    public static Dictionary<(Type, string), PropertyInfo> BuildPropertyCache(params Type[] types)
    {
        var dict = new Dictionary<(Type, string), PropertyInfo>();

        foreach (var type in types)
        {
            foreach (var prop in type.GetProperties(
                         BindingFlags.Public | BindingFlags.Instance))
            {
                dict[(type, prop.Name)] = prop;
            }
        }

        return dict;
    }
}