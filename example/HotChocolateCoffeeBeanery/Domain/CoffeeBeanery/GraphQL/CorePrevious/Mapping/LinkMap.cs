using System;
using System.Linq.Expressions;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class LinkMap<TModel, TEntity>
    {
        public Expression<Func<TModel, object>> SourceKey { get; set; }
        public Expression<Func<TEntity, object>> EntityKey { get; set; }
    }
}