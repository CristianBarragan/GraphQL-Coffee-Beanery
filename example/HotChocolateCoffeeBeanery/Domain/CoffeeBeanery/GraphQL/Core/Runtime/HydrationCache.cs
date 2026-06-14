namespace CoffeeBeanery.GraphQL.Core.Runtime;

public sealed class HydrationCache
{
    private readonly Dictionary<string, object> _cache = new();

    public bool TryGet<T>(string key, out Func<object[], T> projector)
    {
        if (_cache.TryGetValue(key, out var obj))
        {
            projector = (Func<object[], T>)obj;
            return true;
        }

        projector = null!;
        return false;
    }

    public void Store<T>(string key, Func<object[], T> projector)
    {
        _cache[key] = projector;
    }
}