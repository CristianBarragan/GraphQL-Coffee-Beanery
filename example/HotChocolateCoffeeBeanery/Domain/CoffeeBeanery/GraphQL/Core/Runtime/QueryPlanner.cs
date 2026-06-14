// IQueryPlanner.cs — align interface to QueryPlanner.Build's actual signature
using CoffeeBeanery.GraphQL.Core.Runtime;

namespace CoffeeBeanery.GraphQL.Core.Contracts;

public interface IQueryPlanner
{
    QueryPlan Build(GraphIL graph, string rootAlias);
    QueryPlan Build(GraphIL graph, string rootAlias, HashSet<string> selectedAliases);
}

public sealed class QueryPlanner : IQueryPlanner
{
    public QueryPlan Build(GraphIL graph, string rootAlias)
        => Build(graph, rootAlias, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

    public QueryPlan Build(GraphIL graph, string rootAlias, HashSet<string> selectedAliases)
    {
        if (!graph.Nodes.TryGetValue(rootAlias, out var root))
            throw new InvalidOperationException(
                $"Root alias '{rootAlias}' not found in GraphIL");

        var nodes   = new Dictionary<string, PlanNode>(StringComparer.OrdinalIgnoreCase);
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var stack   = new Stack<string>();

        // Find the real root — skip model nodes, use first entity child instead
        var effectiveRoot = rootAlias;
        if (root.IsModel && graph.EdgesBySourceAlias.TryGetValue(rootAlias, out var rootEdges))
        {
            var firstEntityEdge = rootEdges.FirstOrDefault(e =>
                graph.Nodes.TryGetValue(e.ToAlias, out var n) && n.IsEntity);
            if (firstEntityEdge != null)
                effectiveRoot = firstEntityEdge.ToAlias;
        }

        stack.Push(effectiveRoot);

        // Phase 1: walk reachable entity nodes, filtered to selected aliases
        while (stack.Count > 0)
        {
            var alias = stack.Pop();

            if (!visited.Add(alias)) continue;
            if (!graph.Nodes.TryGetValue(alias, out var node)) continue;

            // Skip model nodes — they're GraphQL wrappers, not tables
            if (node.IsModel) continue;

            // Skip nodes not in the selection set (unless no selection set provided)
            if (selectedAliases.Count > 0 && !selectedAliases.Contains(alias)) continue;

            nodes[alias] = new PlanNode(
                alias,
                node.TableName,
                node.Schema ?? "public",
                Required:   true,
                Columns:    node.Columns.ToList(),
                EntityType: node.EntityType);

            if (graph.EdgesBySourceAlias.TryGetValue(alias, out var edges))
            {
                foreach (var edge in edges)
                {
                    if (!visited.Contains(edge.ToAlias))
                        stack.Push(edge.ToAlias);
                }
            }
        }

        // Phase 2: joins only between nodes that are in the plan
        var joins = new List<PlanJoin>();

        foreach (var (fromAlias, edges) in graph.EdgesBySourceAlias)
        {
            if (!nodes.ContainsKey(fromAlias)) continue;

            foreach (var edge in edges)
            {
                if (!nodes.ContainsKey(edge.ToAlias)) continue;
                if (string.IsNullOrEmpty(edge.FromColumn) ||
                    string.IsNullOrEmpty(edge.ToColumn)) continue;

                joins.Add(new PlanJoin(
                    FromAlias:  edge.FromAlias,
                    ToAlias:    edge.ToAlias,
                    FromColumn: edge.FromColumn,
                    ToColumn:   edge.ToColumn));
            }
        }

        // Effective root must be in nodes
        if (!nodes.ContainsKey(effectiveRoot))
            throw new InvalidOperationException(
                $"Effective root '{effectiveRoot}' not found after node walk");

        return new QueryPlan
        {
            Nodes     = nodes,
            Joins     = joins,
            RootAlias = nodes[effectiveRoot]
        };
    }
}