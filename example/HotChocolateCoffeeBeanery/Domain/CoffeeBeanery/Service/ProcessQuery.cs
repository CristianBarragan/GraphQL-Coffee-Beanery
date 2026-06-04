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
        var types = parameters.SqlStructure.SplitOnDapper.Values.ToList();
        var splitOn = parameters.SqlStructure.SplitOnDapper.Keys.ToList();

        var query = parameters.SqlStructure.SqlUpsert + ";" +
                    parameters.SqlStructure.SqlQuery;

        var models = new List<M>();

        int? totalCount = null;
        int? totalPageRecords = null;

        await _db.OpenAsync(ct);
        var tx = await _db.BeginTransactionAsync(ct);

        Console.WriteLine("===== GENERATED TYPES =====");

        foreach (var type in types)
        {
            Console.WriteLine(type.Name);
        }
        
        try
        {
            await _db.QueryAsync(
                query,
                types.ToArray(),
                (object[] map) =>
                {
                    var set = MappingConfiguration(models, parameters.SqlStructure, map, SqlNodeRegistry.EntityTypes, types, parameters.SqlStructure.SqlNodesApplied,
                        parameters.SqlStructure.RelativeTree, parameters.SqlStructure.ModelTrees, parameters.SqlStructure.EntityTrees);
                    models = set.models;

                    return 0;
                },
                splitOn: string.Join(",", splitOn),
                transaction: tx);

            await tx.CommitAsync(ct);

            return (
                models,
                parameters.SqlStructure.Pagination?.StartCursor,
                parameters.SqlStructure.Pagination?.EndCursor,
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
    
    public virtual (List<M> models, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)
        MappingConfiguration(List<M> models, SqlStructure sqlStructure, object[] map, List<Type> allTypes, List<Type> types,
            Dictionary<string, SqlNode> sqlNodesApplied, NodeTree relativeTree, Dictionary<string, NodeTree> modelTrees, Dictionary<string, NodeTree> entityTrees)
    {
        throw new NotImplementedException();
    }

    private static Dictionary<int, string> BuildAliasIndex(
        Dictionary<string, Type> entityMapping)
    {
        var dict = new Dictionary<int, string>();
        int i = 0;

        foreach (var kv in entityMapping)
        {
            dict[i++] = kv.Key;
        }

        return dict;
    }
}