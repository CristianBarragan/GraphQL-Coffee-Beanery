using System.Collections.Generic;
using System.Linq;

namespace CoffeeBeanery.GraphQL.Core.Helper;

public static class BulkMapperExtensions
{
    public static Action<TSource, TDestination> CompileMap<TSource, TDestination>(
        this IEnumerable<PropertyMapping<TSource, TDestination>> mappings)
    {
        return BulkMapper.Compile(mappings.ToArray());
    }
}