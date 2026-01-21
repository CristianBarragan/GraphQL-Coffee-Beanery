using System.Collections.Generic;
using System.Linq.Expressions;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Helper
{
    public static class PropertyMappingFactory
    {
        public static PropertyMapping<TSource, TDestination> Create<TSource, TDestination>(
            Expression<Func<TSource, object>> source,
            Expression<Func<TDestination, object>> destination,
            string relationshipKey = null,
            List<UpsertKey> upsertKeys = null,
            Dictionary<Enum, Enum> fromEnum = null,
            Dictionary<Enum, Enum> toEnum = null,
            List<LinkKey> linkKeys = null)
            where TSource : class
            where TDestination : class
        {
            return new PropertyMapping<TSource, TDestination>(
                source, destination,
                relationshipKey,
                upsertKeys,
                fromEnum,
                toEnum,
                linkKeys);
        }
    }
}