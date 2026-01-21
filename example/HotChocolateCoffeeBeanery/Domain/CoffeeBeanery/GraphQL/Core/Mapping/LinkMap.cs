using System;
using System.Linq.Expressions;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class LinkMap<TModel, TEntity>
    {
        public Expression<Func<TModel, object>> SourceKey { get; set; } = default!;
        public Expression<Func<TEntity, object>> EntityKey { get; set; } = default!;
    }
}