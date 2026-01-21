using System;
using System.Linq.Expressions;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class FieldMap<TModel, TEntity>
    {
        public Expression<Func<TModel, object>> Source { get; set; }
        public Expression<Func<TEntity, object>> Destination { get; set; }
    }
}