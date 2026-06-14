using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.Service;
using Dapper;
using Npgsql;

public class ProcessQuery<M> :
    IQuery<ProcessQueryParameters, List<object[]>>
    where M : class
{
    private readonly ILogger<ProcessQuery<M>> _logger;
    private readonly NpgsqlDataSource _db;

    public ProcessQuery(
        ILoggerFactory loggerFactory,
        NpgsqlDataSource db)
    {
        _logger = loggerFactory.CreateLogger<ProcessQuery<M>>();
        _db = db;
    }

    public async Task<List<object[]>> ExecuteAsync(
        ProcessQueryParameters parameters,
        CancellationToken ct)
    {
        var ctx = parameters.Context;
        var result = new List<object[]>();

        await using var connection = await AgeConnectionFactory.OpenAsync(_db);

        var splitTypes = ctx.SplitOnDapper?.Count > 0
            ? ctx.SplitOnDapper.OrderBy(x => x.Key).Select(x => x.Value).ToArray()
            : Array.Empty<Type>();

        var splitOn = string.Join(",",
            ctx.SplitOnDapper?.OrderBy(x => x.Key).Select(x => x.Key)
            ?? Enumerable.Empty<string>());

        await connection.QueryAsync(
            ctx.SelectSql,
            splitTypes,
            (object[] row) =>
            {
                result.Add(row);
                return 0;
            },
            splitOn: splitOn);

        return result;
    }
}