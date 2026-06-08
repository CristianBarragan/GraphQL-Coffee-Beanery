using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Runtime;
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
        var context = parameters.Context;
        var types   = context.SplitOnDapper.Values.ToList();
        var splitOn = context.SplitOnDapper.Keys.ToList();

        var query = context.UpsertSql + ";" +
                    context.SelectSql;

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
                context,
                rowMatrix,
                types
                );

            return (
                result.models,
                result.startCursor  ?? context.Pagination?.StartCursor,
                result.endCursor    ?? context.Pagination?.EndCursor,
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
            SqlCompilationContext context,
            List<object[]>                  rowMatrix,
            List<Type>                      types)
    {
        throw new NotImplementedException();
    }
}