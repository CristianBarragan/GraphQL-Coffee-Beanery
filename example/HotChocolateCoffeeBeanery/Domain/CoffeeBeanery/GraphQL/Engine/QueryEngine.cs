using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.Service;
using Microsoft.Extensions.DependencyInjection;
using IQueryOptimizer = CoffeeBeanery.GraphQL.Core.Contracts.IQueryOptimizer;

namespace CoffeeBeanery.GraphQL.Engine;

public interface IQueryEngine
{
    Task<IList<T>> ExecuteAsync<T>(
        QueryRequest request,
        CancellationToken ct) where T : class, new();
}

public class QueryEngine : IQueryEngine
{
    private readonly IQueryOptimizer  _optimizer;
    private readonly IQueryExecutor   _executor;
    private readonly IHydrationEngine _hydrator;
    private readonly ExecutionCache   _cache;
    private readonly QueryTraceCollector _traces;
    private readonly IServiceProvider _services;

    public QueryEngine(
        IQueryOptimizer optimizer,
        IQueryExecutor executor,
        IHydrationEngine hydrator,
        ExecutionCache cache,
        QueryTraceCollector traces,
        IServiceProvider services)
    {
        _optimizer = optimizer;
        _executor  = executor;
        _hydrator  = hydrator;
        _cache     = cache;
        _traces    = traces;
        _services  = services;
    }

    public async Task<IList<T>> ExecuteAsync<T>(
        QueryRequest request,
        CancellationToken ct) where T : class, new()
    {
        var plan = request.QueryPlan
            ?? throw new InvalidOperationException("QueryRequest.Plan must be set");

        var shapeKey = request.OptimizationContext?.ShapeKey ?? typeof(T).FullName!;

        var cachedPlan = _cache.GetPlan(shapeKey);
        if (cachedPlan != null)
            plan = cachedPlan;
        else
            _cache.StorePlan(shapeKey, plan);

        // 1. Execute SQL → raw Dapper rows
        var rows = await _executor.ExecuteWithSplitAsync(
            request.ExecutionContext,
            request.Sql,
            request.HydrationContext.SplitOnDapper,
            ct,
            _traces);

        var hydrationContext = request.HydrationContext;

        if (hydrationContext == null || rows.Count == 0)
            return new List<T>();

        // 2. Tree-walk: build model graph (navigations, collections)
        var handler = _services.GetRequiredService<QueryHandler<T>>();

        var parameters = new ProcessQueryParameters
        {
            Context = new HydrationRuntimeContext
            {
                Graph         = hydrationContext.Graph,
                SplitOnDapper = hydrationContext.SplitOnDapper,
                PaginationContext    = hydrationContext.PaginationContext
            }
        };

        var (wrappers, _, _, _, _) = await handler.HydrateAsync(rows, parameters, ct);

        return wrappers;
    }
}