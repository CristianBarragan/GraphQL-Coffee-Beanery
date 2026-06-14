using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.GraphQL;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public class HydrationContextBuilder
{
    public static List<MemberMapping> BuildMappings(NodeTree tree)
    {
        var type = tree.EntityType;
        var result = new List<MemberMapping>();

        var index = 0;

        foreach (var field in tree.NodeMap.FieldMaps)
        {
            var prop = type.GetProperty(
                field.DestinationName,
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.IgnoreCase);

            if (prop == null || !prop.CanWrite)
                continue;

            result.Add(new MemberMapping(
                index++,
                prop,
                prop.PropertyType));
        }

        return result;
    }
    
    public static string BuildCacheKey(NodeTree tree, ProjectionMap projection)
    {
        return $"{tree.EntityType.FullName}|" +
               string.Join(",",
                   projection.Columns
                       .OrderBy(x => x.Index)
                       .Select(x => $"{x.Property}:{x.Index}"));
    }
}