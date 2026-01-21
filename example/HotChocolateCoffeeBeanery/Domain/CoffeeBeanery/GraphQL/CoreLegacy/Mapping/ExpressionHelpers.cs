using System;
using System.Linq.Expressions;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public static class ExpressionHelpers
    {
        public static string GetPropertyName(Expression expr)
        {
            if (expr is LambdaExpression lambda)
            {
                if (lambda.Body is MemberExpression member)
                    return member.Member.Name;

                if (lambda.Body is UnaryExpression unary &&
                    unary.Operand is MemberExpression m2)
                    return m2.Member.Name;
            }
            throw new InvalidOperationException("Unsupported expression");
        }
    }
}