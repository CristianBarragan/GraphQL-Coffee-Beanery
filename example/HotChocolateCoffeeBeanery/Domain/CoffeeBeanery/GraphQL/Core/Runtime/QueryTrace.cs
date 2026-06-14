namespace CoffeeBeanery.GraphQL.Core.Runtime;

public class QueryTrace
{
    public string QueryShapeKey { get; set; }

    public string PlanId { get; set; }

    public long PlanningTimeMs { get; set; }

    public long ExecutionTimeMs { get; set; }

    public long HydrationTimeMs { get; set; }

    public int RowCount { get; set; }

    public int JoinCount { get; set; }

    public bool CacheHit { get; set; }

    public DateTime Timestamp { get; set; }
}