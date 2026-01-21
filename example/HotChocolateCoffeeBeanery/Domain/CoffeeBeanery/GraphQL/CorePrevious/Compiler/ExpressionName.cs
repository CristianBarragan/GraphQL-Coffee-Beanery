using System.Linq.Expressions;

namespace CoffeeBeanery.GraphQL.Core.Compiler;

internal static class ExpressionName
{
    public static string GetMemberName(LambdaExpression expr)
    {
        Expression body = expr.Body;

        if (body is UnaryExpression u)
            body = u.Operand;

        if (body is MemberExpression m)
            return m.Member.Name;

        throw new InvalidOperationException(
            $"Invalid mapping expression: {expr}");
    }
}
