// IMutationEngine.cs
using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.Service;
using HotChocolate.Language;
using ExecutionContext = CoffeeBeanery.GraphQL.Core.Contracts.ExecutionContext;

namespace CoffeeBeanery.GraphQL.Engine;

public interface IMutationEngine
{
    Task<IList<T>> ExecuteAsync<T>(
        MutationRequest   request,
        CancellationToken ct) where T : class, new();
}

// MutationEngine.cs
public class MutationEngine : IMutationEngine
{
    private readonly IGraphEngine     _graphEngine;
    private readonly IQueryExecutor   _executor;
    private readonly ExecutionCache   _cache;
    private readonly IServiceProvider _services;

    public MutationEngine(
        IGraphEngine     graphEngine,
        IQueryExecutor   executor,
        ExecutionCache   cache,
        IServiceProvider services)
    {
        _graphEngine = graphEngine;
        _executor    = executor;
        _cache       = cache;
        _services    = services;
    }

    public async Task<IList<T>> ExecuteAsync<T>(
        MutationRequest   request,
        CancellationToken ct) where T : class, new()
    {
        if (request.HydrationContext == null)
            return new List<T>();

        // Phase 1: relational inserts — sequential, order matters.
        // Stmts 4 & 5 (FK id resolution via SELECT) depend on stmts 1 & 2 being visible.
        var insertSql = _graphEngine.BuildInsertOnlySql(request.InsertSql);
        // if (!string.IsNullOrWhiteSpace(insertSql))
        // {
        //     Console.WriteLine("=== Phase 1: Relational inserts ===");
        //     Console.WriteLine(insertSql);
        //     await _executor.ExecuteNonQueryAsync(request.ExecutionContext, insertSql, ct);
        // }
        //
        // // Phase 2: graph MERGE — must be its own round trip.
        // // ag_catalog.cypher with a temp table cannot run inside a CTE or alongside other stmts.
        // if (!string.IsNullOrWhiteSpace(request.GraphMergeSql))
        // {
        //     Console.WriteLine("=== Phase 2: Graph MERGE ===");
        //     Console.WriteLine(request.GraphMergeSql);
        //     await _executor.ExecuteNonQueryAsync(request.ExecutionContext, request.GraphMergeSql, ct);
        // }

        // Phase 3: select — reads the now-committed relational + graph state.
        // SelectSql already has WHERE / ORDER / pagination from ProcessService.
        // request.SelectSql = insertSql + request.SelectSql; 
        
        Console.WriteLine("=== Phase 3: Select ===");
        Console.WriteLine(request.SelectSql);

        var rows = await _executor.ExecuteWithSplitAsync(
            request.ExecutionContext,
            request.SelectSql,
            request.HydrationContext.SplitOnDapper,
            ct);

        if (rows.Count == 0)
            return new List<T>();

        // Phase 4: hydrate raw rows into model graph — identical to QueryEngine path.
        var handler    = _services.GetRequiredService<QueryHandler<T>>();
        var parameters = new ProcessQueryParameters
        {
            Context = new HydrationRuntimeContext
            {
                Graph            = request.HydrationContext.Graph,
                SplitOnDapper    = request.HydrationContext.SplitOnDapper,
                PaginationContext = request.HydrationContext.PaginationContext
            }
        };

        var (wrappers, _, _, _, _) = await handler.HydrateAsync(rows.ToList(), parameters, ct);
        return wrappers;
    }
}

public sealed class MutationRequest
{
    public GraphIL           Graph            { get; init; } = null!;
    public string            InsertSql        { get; init; } = "";
    public string            GraphMergeSql    { get; init; } = "";
    public string            SelectSql        { get; init; } = "";
    public QueryPlan?        QueryPlan        { get; init; }
    public HydrationContext? HydrationContext { get; init; }
    public ExecutionContext   ExecutionContext  { get; init; } = null!;
}