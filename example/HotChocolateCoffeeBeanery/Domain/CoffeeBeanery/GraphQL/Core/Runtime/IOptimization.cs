using CoffeeBeanery.GraphQL.Core.Contracts;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public interface IOptimizationPipeline
{
    QueryPlan Run(QueryPlan plan, OptimizationContext context);
}

public class DefaultOptimizationPipeline : IOptimizationPipeline
{
    private readonly IQueryOptimizer[] _optimizers;

    public DefaultOptimizationPipeline(IQueryOptimizer[] optimizers)
    {
        _optimizers = optimizers;
    }

    public QueryPlan Run(QueryPlan plan, OptimizationContext context)
    {
        foreach (var optimizer in _optimizers)
        {
            plan = optimizer.Optimize(plan, context);
        }

        return plan;
    }
}