using CoffeeBeanery.GraphQL.Core.Contracts;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public class ExecutionPartition
{
    public string Key { get; set; }

    public List<PlanNode> Nodes { get; set; } = new();

    public List<PlanJoin> Joins { get; set; } = new();

    public int EstimatedCost { get; set; }

    public bool CanExecuteInParallel { get; set; }
}