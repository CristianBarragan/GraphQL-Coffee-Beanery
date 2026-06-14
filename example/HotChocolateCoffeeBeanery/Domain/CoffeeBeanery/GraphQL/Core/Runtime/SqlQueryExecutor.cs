using CoffeeBeanery.GraphQL.Core.Contracts;
using Dapper;
using Npgsql;
using ExecutionContextNamespace = CoffeeBeanery.GraphQL.Core.Contracts;

public interface IQueryExecutor
{
    Task<IList<object[]>> ExecuteWithSplitAsync(
        ExecutionContextNamespace.ExecutionContext          context,
        string                   sql,
        Dictionary<string, Type> splitOnDapper,
        CancellationToken        ct,
        IQueryTraceCollector?    trace = null);
}

public sealed class QueryExecutor : IQueryExecutor
{
    private readonly NpgsqlDataSource _dataSource;

    public QueryExecutor(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    // Dapper multi-map path — splits each row into typed CLR segments
    public async Task<IList<object[]>> ExecuteWithSplitAsync(
        ExecutionContextNamespace.ExecutionContext          context,
        string                   sql,
        Dictionary<string, Type> splitOnDapper,
        CancellationToken        ct,
        IQueryTraceCollector?    trace = null)
    {
        await using var connection = await _dataSource.OpenConnectionAsync(ct);

        // splitOnDapper is ordered: key = alias (or alias~SplitCol), value = CLR type
        var ordered    = splitOnDapper.ToList();
        var types      = ordered.Select(kv => kv.Value).ToArray();
        var splitOn    = string.Join(",", ordered.Select(kv =>
        {
            // key format is either "alias~ColumnName" or just "alias"
            var parts = kv.Key.Split('~');
            return parts.Length > 1 ? parts[1] : parts[0];
        }));

        trace?.ExecutionStarted(sql);

        var rows = new List<object[]>();

        await connection.QueryAsync(
            sql,
            types,
            objects =>
            {
                rows.Add((object[])objects.Clone());
                return 0;   // return value unused
            },
            splitOn:           splitOn,
            commandTimeout:    context.TimeoutMs / 1000);

        trace?.ExecutionCompleted(rows.Count, 0);

        return rows;
    }
}