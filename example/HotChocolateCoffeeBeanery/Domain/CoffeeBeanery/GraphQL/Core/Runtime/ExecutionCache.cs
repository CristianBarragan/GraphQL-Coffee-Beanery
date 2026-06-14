using System.Collections.Concurrent;
using CoffeeBeanery.GraphQL.Core.Contracts;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public interface IExecutionCache
{
    public QueryPlan? GetPlan(string key);

    public bool TryGetPlan(
        string key,
        out QueryPlan plan);

    public void StorePlan(
        string key,
        QueryPlan plan);

    public bool TryGetProjector(
        string key,
        out Delegate projector);

    public void StoreProjector(
        string key,
        Delegate projector);
}

public sealed class ExecutionCache : IExecutionCache
{
    private readonly ConcurrentDictionary<string, QueryPlan> _plans = new();
    private readonly ConcurrentDictionary<string, Delegate> _projectors = new();

    public QueryPlan? GetPlan(string key)
        => _plans.TryGetValue(key, out var plan)
            ? plan
            : null;

    public bool TryGetPlan(
        string key,
        out QueryPlan plan)
        => _plans.TryGetValue(key, out plan);

    public void StorePlan(
        string key,
        QueryPlan plan)
        => _plans[key] = plan;

    public bool TryGetProjector(
        string key,
        out Delegate projector)
        => _projectors.TryGetValue(key, out projector);

    public void StoreProjector(
        string key,
        Delegate projector)
        => _projectors[key] = projector;
}