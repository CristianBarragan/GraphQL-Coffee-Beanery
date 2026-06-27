using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class GraphMutationPlanBuilder
{
    public static ExecutionPlan Build(string rootAlias, IValueNode node)
    {
        var plan = new ExecutionPlan();
        var nextId = 0;

        var (resolvedAlias, innerNode) = ResolveWrapperRoot(rootAlias, node);

        var root = NewNode(plan, ref nextId, resolvedAlias, parentId: null, fieldName: null);
        plan.RootNodeId = root.Id;

        WalkNode(plan, ref nextId, root, innerNode);

        return plan;
    }

    // Mirrors GraphQueryPlanBuilder.ResolveWrapperRoot: skips past a model-only wrapper
    // argument (e.g. Wrapper, where exactly one field is populated per mutation - signaled
    // by a sibling enum/discriminator argument, not by the shape of this value itself) so
    // plan.RootNodeId always lands on the real entity being written, with that one populated
    // field's value becoming the node to walk from.
    private static (string Alias, IValueNode Node) ResolveWrapperRoot(string rootAlias, IValueNode node)
    {
        if (NodeRegistry.FrozenEntityTrees.ContainsKey(rootAlias))
            return (rootAlias, node);

        if (node is not ObjectValueNode obj)
            return (rootAlias, node);

        foreach (var f in obj.Fields)
        {
            var name = f.Name.Value;

            if (f.Value is NullValueNode)
                continue;

            if (NodeRegistry.FrozenEdgeByAliasAndField.TryGetValue((rootAlias, name), out var edge))
                return ResolveWrapperRoot(edge.ToAlias, f.Value);
        }

        return (rootAlias, node);
    }

    private static ExecutionNode NewNode(ExecutionPlan plan, ref int nextId, string alias, int? parentId, string? fieldName)
    {
        var n = new ExecutionNode
        {
            Id = nextId++,
            Alias = alias,
            ParentId = parentId,
            FieldName = fieldName,
            IsEntity = NodeRegistry.FrozenEntityTrees.ContainsKey(alias)
        };

        plan.Nodes[n.Id] = n;
        plan.NodeOrder.Add(n.Id);

        if (parentId is { } pid && !plan.Edges.ContainsKey(pid))
            plan.Edges[pid] = new List<ExecutionEdge>();

        return n;
    }

    private static void WalkNode(ExecutionPlan plan, ref int nextId, ExecutionNode current, IValueNode node)
    {
        if (node is ListValueNode list)
        {
            foreach (var item in list.Items)
            {
                if (item is ObjectValueNode)
                {
                    var sibling = NewNode(plan, ref nextId, current.Alias, current.ParentId, current.FieldName);
                    WalkNode(plan, ref nextId, sibling, item);
                }
                else
                {
                    WalkNode(plan, ref nextId, current, item);
                }
            }
            return;
        }

        if (node is not ObjectValueNode obj)
            return;

        foreach (var f in obj.Fields)
        {
            var name = f.Name.Value;

            if (f.Value is ObjectValueNode or ListValueNode)
            {
                if (NodeRegistry.FrozenEdgeByAliasAndField.TryGetValue((current.Alias, name), out var edge))
                {
                    var child = NewNode(plan, ref nextId, edge.ToAlias, current.Id, name);

                    plan.Edges[current.Id].Add(new ExecutionEdge
                    {
                        From = current.Id,
                        To = child.Id,
                        FieldName = name,
                        Kind = edge.Kind,
                        FromColumn = edge.FromColumn,
                        ToColumn = edge.ToColumn
                    });

                    WalkNode(plan, ref nextId, child, f.Value);
                }

                continue;
            }

            var raw = f.Value.Value?.ToString();
            if (raw is null) continue;

            foreach (var (ea, col) in NodeRegistry.ResolveLeaf(current.Alias, name))
                SetValue(current, col, raw);
        }
    }

    private static void SetValue(ExecutionNode node, string column, string value)
    {
        var values = node.Values;
        for (var i = 0; i < values.Count; i++)
        {
            if (values[i].Column == column)
            {
                values[i] = (column, value);
                return;
            }
        }
        values.Add((column, value));
    }
}