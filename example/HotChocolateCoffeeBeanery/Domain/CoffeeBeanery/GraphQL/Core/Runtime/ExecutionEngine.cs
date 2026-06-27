namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class ExecutionEngine
{
    public static void Traverse(ExecutionPlan plan, Action<ExecutionNode, ExecutionEdge?> visit)
    {
        if (!plan.Nodes.TryGetValue(plan.RootNodeId, out var root))
            return;

        visit(root, null);
        TraverseChildren(plan, plan.RootNodeId, visit);
    }

    private static void TraverseChildren(ExecutionPlan plan, int nodeId, Action<ExecutionNode, ExecutionEdge?> visit)
    {
        if (!plan.Edges.TryGetValue(nodeId, out var edges))
            return;

        for (var i = 0; i < edges.Count; i++)
        {
            var edge = edges[i];

            if (!plan.Nodes.TryGetValue(edge.To, out var child))
                continue;

            visit(child, edge);
            TraverseChildren(plan, child.Id, visit);
        }
    }
}