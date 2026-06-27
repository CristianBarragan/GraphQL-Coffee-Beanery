namespace CoffeeBeanery.GraphQL.Core.Runtime;

public sealed class ExecutionContext
{
    public ExecutionPlan Plan { get; init; }

    public Dictionary<int, object> NodeState { get; } = new();
}