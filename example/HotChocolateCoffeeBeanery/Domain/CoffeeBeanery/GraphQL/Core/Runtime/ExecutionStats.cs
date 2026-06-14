namespace CoffeeBeanery.GraphQL.Core.Runtime;

public class ExecutionStats
{
    public string QueryShapeKey { get; set; }

    public long ExecutionTimeMs { get; set; }

    public int RowCount { get; set; }

    public int JoinCount { get; set; }

    public DateTime ExecutedAt { get; set; }
}