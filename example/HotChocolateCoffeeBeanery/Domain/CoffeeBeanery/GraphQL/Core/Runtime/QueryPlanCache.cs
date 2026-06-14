using System.Text.Json;
using CoffeeBeanery.GraphQL.Core.Contracts;
using Path = System.IO.Path;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public interface IQueryPlanCache
{
    public QueryPlan Get(string key);

    public void Store(string key, QueryPlan plan);

}

public class QueryPlanCache : IQueryPlanCache
{
    private readonly Dictionary<string, QueryPlan> _memory = new();
    private readonly string _folder = "query-plans";

    public QueryPlanCache()
    {
        if (!Directory.Exists(_folder))
            Directory.CreateDirectory(_folder);
    }

    public QueryPlan Get(string key)
    {
        if (_memory.TryGetValue(key, out var plan))
            return plan;

        var file = Path.Combine(_folder, $"{key}.json");

        if (!File.Exists(file))
            return null;

        plan = JsonSerializer.Deserialize<QueryPlan>(File.ReadAllText(file));

        _memory[key] = plan;
        return plan;
    }

    public void Store(string key, QueryPlan plan)
    {
        _memory[key] = plan;

        var file = Path.Combine(_folder, $"{key}.json");
        File.WriteAllText(file, JsonSerializer.Serialize(plan));
    }
}