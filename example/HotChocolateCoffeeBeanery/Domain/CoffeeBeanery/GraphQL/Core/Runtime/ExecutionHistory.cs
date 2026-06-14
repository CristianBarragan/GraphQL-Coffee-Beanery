namespace CoffeeBeanery.GraphQL.Core.Runtime;

public class ExecutionHistory
{
    private readonly List<ExecutionStats> _stats = new();

    public void Record(ExecutionStats stat)
    {
        _stats.Add(stat);
    }

    public IEnumerable<ExecutionStats> Get(string key)
        => _stats.Where(x => x.QueryShapeKey == key);

    public double AvgTime(string key)
        => Get(key).DefaultIfEmpty().Average(x => x?.ExecutionTimeMs ?? 0);
}