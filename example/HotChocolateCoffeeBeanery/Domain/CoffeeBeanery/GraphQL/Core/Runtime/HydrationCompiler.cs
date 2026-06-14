using System.Linq.Expressions;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Contracts;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class HydrationCompiler
{
    public static Func<object[], T> Build<T>(List<SelectColumn> cols)
    {
        var rowParam = Expression.Parameter(typeof(object[]), "row");
        var resultVar = Expression.Variable(typeof(T), "result");

        var expressions = new List<Expression>
        {
            Expression.Assign(resultVar, Expression.New(typeof(T)))
        };

        foreach (var col in cols.OrderBy(x => x.Index))
        {
            var prop = typeof(T).GetProperty(
                col.Property,
                BindingFlags.Public |
                BindingFlags.Instance |
                BindingFlags.IgnoreCase);

            if (prop == null || !prop.CanWrite)
                continue;

            var value = Expression.Convert(
                Expression.ArrayIndex(rowParam, Expression.Constant(col.Index)),
                prop.PropertyType);

            expressions.Add(
                Expression.Assign(Expression.Property(resultVar, prop), value));
        }

        expressions.Add(resultVar);

        var body = Expression.Block(new[] { resultVar }, expressions);

        return Expression.Lambda<Func<object[], T>>(body, rowParam).Compile();
    }
}