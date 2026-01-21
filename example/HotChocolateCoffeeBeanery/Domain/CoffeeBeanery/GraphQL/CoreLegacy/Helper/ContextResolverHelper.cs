using System;
using System.Linq.Expressions;

namespace CoffeeBeanery.GraphQL.Core.Helper
{
    public static class ContextResolverHelper
    {
        public static string GetPropertyName(Expression expr)
        {
            if (expr is MemberExpression m)
                return m.Member.Name;

            if (expr is UnaryExpression u && u.Operand is MemberExpression m2)
                return m2.Member.Name;

            throw new InvalidOperationException("Unsupported expression");
        }
    }
}