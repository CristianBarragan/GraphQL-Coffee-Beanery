using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Relationships;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Helper
{
    public sealed class PropertyMapping<TSource, TDestination>
    {
        public Expression<Func<TSource, object>> SourceExpression { get; }
        public Expression<Func<TDestination, object>> DestinationExpression { get; }

        public RelationshipKey Relationship { get; init; }
        public List<UpsertKey> UpsertKeys { get; init; }
        public List<LinkKey> LinkKeys { get; init; }
        public Dictionary<Enum, Enum>? FromEnum { get; }
        public Dictionary<Enum, Enum>? ToEnum { get; }

        public PropertyMapping(
            Expression<Func<TSource, object>> source,
            Expression<Func<TDestination, object>> destination,
            RelationshipKey relationship = null,
            List<UpsertKey> upsertKeys = null,
            Dictionary<Enum, Enum>? fromEnum = null,
            Dictionary<Enum, Enum>? toEnum = null,
            List<LinkKey> linkKeys = null)
        {
            SourceExpression = source;
            DestinationExpression = destination;
            Relationship = relationship;
            UpsertKeys = upsertKeys ?? new List<UpsertKey>();
            FromEnum = fromEnum;
            ToEnum = toEnum;
            LinkKeys = linkKeys ?? new List<LinkKey>();
        }
    }
}