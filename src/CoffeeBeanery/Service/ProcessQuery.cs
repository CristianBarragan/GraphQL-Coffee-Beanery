using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using Dapper;
using Npgsql;

namespace CoffeeBeanery.Service;

public class ProcessQuery<M> : IQuery<ProcessQueryParameters,
    (List<M> list, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>
    where M : class
{
    private readonly ILogger<ProcessQuery<M>> _logger;
    private readonly NpgsqlConnection _db;

    public ProcessQuery(
        ILoggerFactory loggerFactory,
        NpgsqlConnection db)
    {
        _logger = loggerFactory.CreateLogger<ProcessQuery<M>>();
        _db = db;
    }

    public async Task<(List<M>, int?, int?, int?, int?)> ExecuteAsync(
        ProcessQueryParameters parameters,
        CancellationToken ct)
    {
        var types   = parameters.SqlStructure.SplitOnDapper.Values.ToList();
        var splitOn = parameters.SqlStructure.SplitOnDapper.Keys.ToList();

        var query = parameters.SqlStructure.SqlUpsert + ";" +
                    parameters.SqlStructure.SqlQuery;

        if (_db.State != System.Data.ConnectionState.Open)
            await _db.OpenAsync(ct);

        NpgsqlTransaction? tx = null;

        try
        {
            tx = await _db.BeginTransactionAsync(ct);

            var rowMatrix = new List<object[]>();

            await _db.QueryAsync(
                query,
                types.ToArray(),
                (object[] row) =>
                {
                    rowMatrix.Add((object[])row.Clone());
                    return 0;
                },
                splitOn:     string.Join(",", splitOn),
                transaction: tx);

            await tx.CommitAsync(ct);

            var result = MappingConfiguration(
                rowMatrix,
                parameters.SqlStructure,
                SqlNodeRegistry.EntityTypes,
                types,
                parameters.SqlStructure.SqlNodesApplied,
                parameters.SqlStructure.RelativeTree,
                parameters.SqlStructure.ModelTrees,
                parameters.SqlStructure.EntityTrees);

            return (
                result.models,
                result.startCursor  ?? parameters.SqlStructure.Pagination?.StartCursor,
                result.endCursor    ?? parameters.SqlStructure.Pagination?.EndCursor,
                result.totalCount,
                result.totalPageRecords);
        }
        catch (Exception ex)
        {
            if (tx is not null)
            {
                try
                {
                    await tx.RollbackAsync(CancellationToken.None);
                }
                catch (InvalidOperationException)
                {
                }
                catch (Exception rollbackEx)
                {
                    _logger.LogWarning(rollbackEx, "Rollback attempt failed");
                }
            }

            _logger.LogError(ex, "ProcessQuery failed");
            return ([], 0, 0, 0, 0);
        }
        finally
        {
            await _db.CloseAsync();
        }
    }

    public virtual (List<M> models,
                    int? startCursor,
                    int? endCursor,
                    int? totalCount,
                    int? totalPageRecords)
        MappingConfiguration(
            List<object[]>                  rowMatrix,
            SqlStructure                    sqlStructure,
            List<Type>                      allTypes,
            List<Type>                      types,
            Dictionary<string, SqlNode>     sqlNodesApplied,
            NodeTree                        relativeTree,
            Dictionary<string, NodeTree>    modelTrees,
            Dictionary<string, NodeTree>    entityTrees)
    {
        throw new NotImplementedException();
    }
}