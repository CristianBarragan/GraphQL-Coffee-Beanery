namespace CoffeeBeanery.GraphQL.Core.Debug;

public class QueryPlanDebug
{
    public List<string> VisitedEntities { get; set; } = new();
    public List<string> IncludedJoins { get; set; } = new();
    public List<string> SkippedJoins { get; set; } = new();
    public List<string> RequiredFields { get; set; } = new();
}