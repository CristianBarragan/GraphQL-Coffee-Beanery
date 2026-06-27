using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class GraphQueryPlanBuilder
{
    private static readonly HashSet<string> ConnectionWrapperFields =
        new(StringComparer.OrdinalIgnoreCase) { "nodes", "edges" };

    private static readonly HashSet<string> ConnectionMetaFields =
        new(StringComparer.OrdinalIgnoreCase) { "pageInfo", "totalCount" };

    public static ExecutionPlan Build(string rootAlias, SelectionSetNode? set)
    {
        var plan = new ExecutionPlan();
        var nextId = 0;

        var (resolvedAlias, innerSet) = ResolveWrapperRoot(rootAlias, set);

        var root = NewNode(plan, ref nextId, resolvedAlias, parentId: null, fieldName: null);
        plan.RootNodeId = root.Id;

        SeedPrimaryKey(root);

        if (innerSet != null)
            WalkSet(plan, ref nextId, root, innerSet);

        return plan;
    }

    private static (string Alias, SelectionSetNode? Set) ResolveWrapperRoot(string rootAlias, SelectionSetNode? set)
    {
        if (set is null)
            return (rootAlias, set);

        if (NodeRegistry.FrozenEntityTrees.ContainsKey(rootAlias))
            return (rootAlias, set);

        foreach (var s in set.Selections)
        {
            if (s is not FieldNode f) continue;

            var name = f.Name.Value;

            if (ConnectionMetaFields.Contains(name))
                continue;

            if (ConnectionWrapperFields.Contains(name))
            {
                if (f.SelectionSet != null)
                    return ResolveWrapperRoot(rootAlias, f.SelectionSet);
                continue;
            }

            if (NodeRegistry.FrozenEdgeByAliasAndField.TryGetValue((rootAlias, name), out var edge)
                && f.SelectionSet != null)
            {
                var targetAlias = edge.ToAlias;

                if (NodeRegistry.FrozenModelTrees.TryGetValue(targetAlias, out var targetModel)
                    && targetModel.EntityType is not null
                    && NodeRegistry.FrozenEntityTrees.ContainsKey(targetModel.EntityType.Name))
                {
                    return (targetModel.EntityType.Name, f.SelectionSet);
                }

                return ResolveWrapperRoot(targetAlias, f.SelectionSet);
            }
        }

        return (rootAlias, set);
    }

    private static ExecutionNode NewNode(ExecutionPlan plan, ref int nextId, string alias, int? parentId, string? fieldName)
    {
        var node = new ExecutionNode
        {
            Id = nextId++,
            Alias = alias,
            ParentId = parentId,
            FieldName = fieldName,
            IsEntity = NodeRegistry.FrozenEntityTrees.ContainsKey(alias)
        };

        plan.Nodes[node.Id] = node;
        plan.NodeOrder.Add(node.Id);

        if (parentId is { } pid && !plan.Edges.ContainsKey(pid))
            plan.Edges[pid] = new List<ExecutionEdge>();

        return node;
    }

    private static void SeedPrimaryKey(ExecutionNode node)
    {
        if (!node.Columns.Contains("Id"))
            node.Columns.Add("Id");
    }

    private static void WalkSet(ExecutionPlan plan, ref int nextId, ExecutionNode current, SelectionSetNode set)
    {
        foreach (var s in set.Selections)
        {
            if (s is not FieldNode f) continue;

            var name = f.Name.Value;

            if (ConnectionMetaFields.Contains(name))
                continue;

            if (ConnectionWrapperFields.Contains(name))
            {
                if (f.SelectionSet != null)
                    WalkSet(plan, ref nextId, current, f.SelectionSet);
                continue;
            }

            if (NodeRegistry.FrozenEdgeByAliasAndField.TryGetValue((current.Alias, name), out var edge))
            {
                var child = NewNode(plan, ref nextId, edge.ToAlias, current.Id, name);

                if (child.IsEntity)
                    SeedPrimaryKey(child);

                plan.Edges[current.Id].Add(new ExecutionEdge
                {
                    From = current.Id,
                    To = child.Id,
                    FieldName = name,
                    Kind = edge.Kind,
                    FromColumn = edge.FromColumn,
                    ToColumn = edge.ToColumn
                });

                if (f.SelectionSet != null)
                    WalkSet(plan, ref nextId, child, f.SelectionSet);

                continue;
            }

            var leaf = NodeRegistry.ResolveLeaf(current.Alias, name);
            if (leaf.Count > 0)
            {
                foreach (var (ea, col) in leaf)
                {
                    var target = string.Equals(ea, current.Alias, StringComparison.OrdinalIgnoreCase)
                        ? current
                        : FindAncestorByAlias(plan, current, ea) ?? current;

                    if (!target.Columns.Contains(col))
                        target.Columns.Add(col);
                }

                if (f.SelectionSet != null)
                    WalkSet(plan, ref nextId, current, f.SelectionSet);

                continue;
            }

            // throw new InvalidOperationException(
            //     $"Field '{name}' on alias '{current.Alias}' is not a registered navigation edge or scalar leaf. " +
            //     "Check NodeRegistry.FrozenEdgeByAliasAndField / FrozenColumnByField registration.");
        }
    }

    private static ExecutionNode? FindAncestorByAlias(ExecutionPlan plan, ExecutionNode start, string alias)
    {
        var node = start;
        while (node != null)
        {
            if (string.Equals(node.Alias, alias, StringComparison.OrdinalIgnoreCase))
                return node;

            node = node.ParentId is { } pid ? plan.Nodes.GetValueOrDefault(pid) : null;
        }
        return null;
    }
}