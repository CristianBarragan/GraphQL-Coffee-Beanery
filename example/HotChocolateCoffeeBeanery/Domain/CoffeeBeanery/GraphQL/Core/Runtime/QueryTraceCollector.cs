using CoffeeBeanery.GraphQL.Core.Contracts;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public sealed class QueryTraceCollector : IQueryTraceCollector
{
    private readonly List<object> _events = new();

    public IReadOnlyList<object> Events => _events;

    public void PlanBuilt(QueryPlan plan)
    {
        _events.Add(new { Type = "PlanBuilt", Plan = plan });
    }

    public void Optimized(QueryPlan original, QueryPlan optimized)
    {
        _events.Add(new { Type = "Optimized", Original = original, Optimized = optimized });
    }

    public void SqlGenerated(string sql)
    {
        _events.Add(new { Type = "SqlGenerated", Sql = sql });
    }

    public void ExecutionStarted(string sql)
    {
        _events.Add(new { Type = "ExecutionStarted", Sql = sql });
    }

    public void ExecutionCompleted(int rowCount, long elapsedMs)
    {
        _events.Add(new { Type = "ExecutionCompleted", RowCount = rowCount, ElapsedMs = elapsedMs });
    }

    public void Error(Exception exception)
    {
        _events.Add(new { Type = "Error", Exception = exception });
    }
}