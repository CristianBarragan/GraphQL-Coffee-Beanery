using System.Data;
using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.Sql;
using Dapper;
using Npgsql;

namespace CoffeeBeanery.Service;

public class ProcessQuery<M> : IQuery<ProcessQueryParameters,
    (List<M> list, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>
    where M : class, new()
{
    private readonly ILogger<ProcessQuery<M>> _logger;
    private readonly NpgsqlConnection _dbConnection;

    private readonly Dictionary<string, M> _edgeDict = new();

    public ProcessQuery(ILoggerFactory loggerFactory, NpgsqlConnection dbConnection)
    {
        _logger = loggerFactory.CreateLogger<ProcessQuery<M>>();
        _dbConnection = dbConnection;
    }

    public async Task<(List<M> list, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>
        ExecuteAsync(ProcessQueryParameters parameters, CancellationToken cancellationToken)
    {
        var types = parameters.SqlStructure.SplitOnDapper.Values.Reverse().ToList();
        var splitOnDict = parameters.SqlStructure.SplitOnDapper;

        if (splitOnDict.Count > 0)
            splitOnDict.RemoveAt(splitOnDict.Count - 1);

        var splitOn = splitOnDict.Reverse().Select(a => a.Key).ToList();

        if (parameters.Pagination.TotalRecordCount.RecordCount > 0 &&
            parameters.Pagination.TotalPageRecords.PageRecords > 0)
        {
            types.Add(typeof(TotalPageRecords));
            types.Add(typeof(TotalRecordCount));

            splitOn.Insert(0, "RowNumber");
            parameters.SqlStructure.Aliases.Add("RowNumber");
        }

        var query = parameters.SqlStructure.SqlUpsert + " ; " + parameters.SqlStructure.SqlQuery;

        int? totalCount = null;
        int? totalPageRecords = null;
        int? startCursor = null;
        int? endCursor = null;

        // 🔥 BUILD MAPPER ONCE
        var mapper = GraphMaterializer.BuildMapper<M>(
            parameters.SqlStructure.SqlNodes,
            parameters.SqlStructure.Trees,
            parameters.SqlStructure.EntityMapping);

        await _dbConnection.OpenAsync(cancellationToken);
        var transaction = await _dbConnection.BeginTransactionAsync(cancellationToken);

        try
        {
            await _dbConnection.QueryAsync(
                query,
                types.ToArray(),
                (object[] map) =>
                {
                    GraphMaterializer.MergeRow(
                        map,
                        parameters.SqlStructure.SqlNodes,
                        parameters.SqlStructure.Trees,
                        parameters.SqlStructure.EntityMapping,
                        mapper,
                        _edgeDict,
                        ref totalCount,
                        ref totalPageRecords);

                    return 0;
                },
                splitOn: string.Join(",", splitOn),
                transaction: transaction,
                commandType: CommandType.Text);

            return (_edgeDict.Values.ToList(), startCursor, endCursor, totalCount, totalPageRecords);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync(cancellationToken);
            _logger.LogError(ex, "Error executing ProcessQuery");
        }
        finally
        {
            await _dbConnection.CloseAsync();
        }

        return ([], 0, 0, 0, 0);
    }
}