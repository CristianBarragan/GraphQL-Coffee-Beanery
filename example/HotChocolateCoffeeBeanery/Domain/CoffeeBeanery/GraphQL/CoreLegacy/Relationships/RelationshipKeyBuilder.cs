using System;
using System.Linq.Expressions;
using CoffeeBeanery.GraphQL.Core.Helper;

namespace CoffeeBeanery.GraphQL.Core.Relationships
{
    public static class RelationshipKeyBuilder
    {
        public static RelationshipKey From<TEntity>(
            Expression<Func<TEntity, object>> destExpression)
        {
            var propName = ContextResolverHelper.GetPropertyName(destExpression);
            var entityName = typeof(TEntity).Name;
            return new RelationshipKey(entityName, propName);
        }
    }
}