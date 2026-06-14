using Dapper;
using Npgsql;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class ExecutionEngine
{
    public static async Task<List<object>> ExecuteAsync(
        List<ExecutionPartition> partitions,
        string sql,
        NpgsqlConnection connection,
        NpgsqlTransaction tx)
    {
        var tasks = new List<Task<List<object>>>();

        foreach (var partition in partitions)
        {
            tasks.Add(ExecutePartition(partition, sql, connection, tx));
        }

        var results = await Task.WhenAll(tasks);

        return results.SelectMany(x => x).ToList();
    }

    private static async Task<List<object>> ExecutePartition(
        ExecutionPartition partition,
        string sql,
        NpgsqlConnection connection,
        NpgsqlTransaction tx)
    {
        var result = new List<object>();

        await connection.QueryAsync(
            sql,
            (object[] row) =>
            {
                // lightweight execution per partition
                result.Add(row);
                return 0;
            },
            transaction: tx
        );

        return result;
    }
}