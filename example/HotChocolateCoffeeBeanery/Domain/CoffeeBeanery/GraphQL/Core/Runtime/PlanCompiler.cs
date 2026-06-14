using CoffeeBeanery.GraphQL.Core.Contracts;
using Dapper;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class PlanCompiler
{
    public static CompiledPlan Compile(QueryPlan plan, string sql)
    {
        return new CompiledPlan
        {
            Execute = async (connection) =>
            {
                var result = new List<object>();

                await connection.QueryAsync(
                    sql,
                    (object[] row) =>
                    {
                        result.Add(row);
                        return 0;
                    });

                return result;
            }
        };
    }
}