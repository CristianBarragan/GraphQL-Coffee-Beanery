using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class SqlSelectBuilder
{
    public static void HandleGraphQL(
        SqlCompilationContext context,
        QueryPlan plan)
    {
        var (sql, projection) = SqlRenderer.Render(plan);

        context.SelectSql = sql;
        context.Projection = projection;
        context.SplitOnDapper = BuildSplitOn(plan);
    }

    private static Dictionary<string, Type> BuildSplitOn(QueryPlan plan)
    {
        var result = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in plan.Nodes.Values)
        {
            result[node.Alias] = node.EntityType;
        }

        return result;
    }
}