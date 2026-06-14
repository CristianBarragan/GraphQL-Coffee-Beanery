using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.Runtime;

public class QueryOptimizer : IQueryOptimizer
{
    public interface IQueryOptimizer
    {
        QueryPlan Optimize(QueryPlan plan, OptimizationContext context);
    }
    
    public QueryPlan Optimize(
        QueryPlan plan,
        OptimizationContext context)
    {
        var stats = context.ExecutionHistory.Get(context.ShapeKey).ToList();

        if (!stats.Any())
            return plan;

        var avgTime = stats.Average(x => x.ExecutionTimeMs);
        var avgRows = stats.Average(x => x.RowCount);

        var newNodes = new Dictionary<string, PlanNode>();

        foreach (var (alias, node) in plan.Nodes)
        {
            var updatedNode = node;

            // RULE 1: slow → reduce columns (safe approximation of join reduction)
            if (avgTime > 500)
            {
                updatedNode = updatedNode with
                {
                    Columns = updatedNode.Columns
                        .Take(updatedNode.Columns.Count / 2)
                        .ToList()
                };
            }

            // RULE 2: high row count → mark required for pruning safety
            if (avgRows > 10_000)
            {
                updatedNode = updatedNode with
                {
                    Required = true
                };
            }

            newNodes[alias] = updatedNode;
        }

        return new QueryPlan
        {
            Nodes = newNodes
        };
    }
}