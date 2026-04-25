using System.Data;
using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Dapper;
using Npgsql;

namespace CoffeeBeanery.Service;

public class ProcessQuery<M> : IQuery<ProcessQueryParameters,
    (List<M> list, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>
    where M : class, new()
{
    private readonly ILogger<ProcessQuery<M>> _logger;
    private readonly NpgsqlConnection _db;

    public ProcessQuery(ILoggerFactory loggerFactory, NpgsqlConnection db)
    {
        _logger = loggerFactory.CreateLogger<ProcessQuery<M>>();
        _db = db;
    }

    public async Task<(List<M>, int?, int?, int?, int?)> ExecuteAsync(
        ProcessQueryParameters parameters,
        CancellationToken ct)
    {
        var types = parameters.SqlStructure.SplitOnDapper.Values.Reverse().ToList();
        var splitOn = parameters.SqlStructure.SplitOnDapper.Keys.Reverse().ToList();

        var query = parameters.SqlStructure.SqlUpsert + ";" + parameters.SqlStructure.SqlQuery;

        var edgeDict = new Dictionary<string, M>();

        int? totalCount = null;
        int? totalPageRecords = null;
        int? startCursor = null;
        int? endCursor = null;

        await _db.OpenAsync(ct);
        var tx = await _db.BeginTransactionAsync(ct);

        try
        {
            await _db.QueryAsync(
                query,
                types.ToArray(),
                (object[] map) =>
                {
                    GraphMaterializer.MergeRow(
                        map,
                        parameters.SqlStructure.SqlNodes,
                        parameters.SqlStructure.ModelTrees,
                        parameters.SqlStructure.EntityMapping,
                        edgeDict,
                        parameters.Model,
                        ref totalCount,
                        ref totalPageRecords);

                    return 0;
                },
                splitOn: string.Join(",", splitOn),
                transaction: tx);

            await tx.CommitAsync(ct);

            return (
                edgeDict.Values.ToList(),
                startCursor,
                endCursor,
                totalCount,
                totalPageRecords
            );
        }
        catch (Exception ex)
        {
            await tx.RollbackAsync(ct);
            _logger.LogError(ex, "ProcessQuery failed");
            return ([], 0, 0, 0, 0);
        }
        finally
        {
            await _db.CloseAsync();
        }
    }
}