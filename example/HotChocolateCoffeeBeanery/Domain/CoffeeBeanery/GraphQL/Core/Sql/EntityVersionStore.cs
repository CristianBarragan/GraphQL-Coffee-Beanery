namespace CoffeeBeanery.GraphQL.Core.Sql;

public class EntityVersionStore
{
    private readonly Dictionary<string, long> _versions = new();

    public long Get(string entity)
    {
        return _versions.TryGetValue(entity, out var v) ? v : 0;
    }

    public long Increment(string entity)
    {
        if (!_versions.ContainsKey(entity))
            _versions[entity] = 0;

        return ++_versions[entity];
    }
}