using System.Collections.Concurrent;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public interface IGraphILCache
{
    GraphIL GetOrBuild(string schemaVersion, Func<GraphIL> factory);
}

public sealed class GraphILCache : IGraphILCache
{
    private readonly ConcurrentDictionary<string, GraphIL> _cache = new();

    public GraphIL GetOrBuild(string schemaVersion, Func<GraphIL> factory)
    {
        return _cache.GetOrAdd(schemaVersion, _ => factory());
    }
}