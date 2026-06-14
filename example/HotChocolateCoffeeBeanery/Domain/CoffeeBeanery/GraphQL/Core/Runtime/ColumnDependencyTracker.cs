namespace CoffeeBeanery.GraphQL.Core.Runtime;

public sealed class ColumnDependencyTracker
{
    private readonly Dictionary<string, HashSet<string>> _map = new();

    public void Add(string alias, string column)
    {
        if (string.IsNullOrWhiteSpace(alias) || string.IsNullOrWhiteSpace(column))
            return;

        if (!_map.TryGetValue(alias, out var set))
        {
            set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            _map[alias] = set;
        }

        set.Add(column);
    }

    public bool Has(string alias)
        => _map.TryGetValue(alias, out var cols) && cols.Count > 0;

    public IReadOnlyCollection<string> Get(string alias)
        => _map.TryGetValue(alias, out var cols)
            ? cols
            : Array.Empty<string>();

    public IReadOnlyDictionary<string, HashSet<string>> Snapshot()
        => _map;
}