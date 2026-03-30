using System.Data;
using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.Sql;
using Dapper;
using Npgsql;

namespace CoffeeBeanery.Service;

public class ProcessQuery<M> : IQuery<ProcessQueryParameters,
    (List<M> list, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>
    where M : class
{
    private readonly ILogger<ProcessQuery<M>> _logger;
    private readonly NpgsqlConnection _dbConnection;
    private List<M> _models;

    public ProcessQuery(ILoggerFactory loggerFactory, NpgsqlConnection dbConnection)
    {
        _logger      = loggerFactory.CreateLogger<ProcessQuery<M>>();
        _dbConnection = dbConnection;
        _models       = new List<M>();
    }

    public async Task<(List<M> list, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>
        ExecuteAsync(ProcessQueryParameters parameters, CancellationToken cancellationToken)
    {
        // Build types list — reversed so Dapper receives them in SQL column order
        var types  = parameters.SqlStructure.SplitOnDapper.Values.Reverse().ToList();
        var splitOnDict = parameters.SqlStructure.SplitOnDapper;

        // Remove last entry before building splitOn string (last type has no split column)
        if (splitOnDict.Count > 0)
            splitOnDict.RemoveAt(splitOnDict.Count - 1);

        var splitOn = splitOnDict.Reverse().Select(a => a.Key).ToList();

        // Append pagination meta-types when counts are known
        if (parameters.Pagination.TotalRecordCount.RecordCount > 0
            && parameters.Pagination.TotalPageRecords.PageRecords > 0)
        {
            types.Add(typeof(TotalPageRecords));
            types.Add(typeof(TotalRecordCount));
            splitOn.Insert(0, "RowNumber");
            parameters.SqlStructure.Aliases.Add("RowNumber");
        }

        var query      = parameters.SqlStructure.SqlUpsert + " ; " + parameters.SqlStructure.SqlQuery;
        var connection = _dbConnection;

        await connection.OpenAsync(cancellationToken);
        var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);

        try
        {
            var result = await connection
                .QueryAsync<(int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>(
                    query,
                    types.ToArray(),
                    map =>
                    {
                        var set  = MappingConfiguration(_models, parameters.SqlStructure, map, types,
                                       parameters.SqlStructure.Aliases);
                        _models = set.models;
                        return (set.startCursor, set.endCursor, set.totalCount, set.totalPageRecords);
                    },
                    splitOn:     string.Join(",", splitOn),
                    transaction: dbTransaction,
                    commandType: CommandType.Text);

            await dbTransaction.CommitAsync(cancellationToken);

            if (result == null || !result.Any())
                return ([], 0, 0, 0, 0);

            var resultList = result.ToList();

            return (
                _models,
                parameters.Pagination.StartCursor > 0
                    ? parameters.Pagination.StartCursor
                    : resultList.Select(s => s.startCursor).FirstOrDefault(),
                parameters.Pagination.EndCursor > 0
                    ? parameters.Pagination.EndCursor
                    : resultList.Select(s => s.endCursor).FirstOrDefault(),
                resultList.Select(s => s.totalCount).FirstOrDefault(),
                resultList.Select(s => s.totalPageRecords).FirstOrDefault()
            );
        }
        catch (Exception ex)
        {
            await dbTransaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error executing ProcessQuery");
        }
        finally
        {
            await connection.CloseAsync();
        }

        return ([], 0, 0, 0, 0);
    }

    public virtual (List<M> models, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)
        MappingConfiguration(List<M> models, SqlStructure sqlStructure, object[] map, List<Type> types, List<string> aliases)
    {
        throw new NotImplementedException();
    }
}