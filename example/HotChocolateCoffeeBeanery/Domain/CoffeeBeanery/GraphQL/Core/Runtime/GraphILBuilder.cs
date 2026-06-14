using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Runtime;

public static class GraphILBuilder
{
    public static GraphIL Build(IEnumerable<NodeMap> maps)
    {
        var graph = new GraphIL
        {
            SchemaVersion = "1",
            Nodes = new Dictionary<string, GraphILNode>(),
            EdgesBySourceAlias = new Dictionary<string, List<GraphILEdge>>(),
            EdgesByTargetAlias = new Dictionary<string, List<GraphILEdge>>()
        };

        foreach (var map in maps)
        {
            var alias = map.Alias;

            graph.Nodes[alias] = new GraphILNode
            {
                Alias = alias,
                EntityType = map.EntityType,
                TableName = map.EntityType?.Name ?? map.ModelType.Name,
                UpsertKeys = map.UpsertKeys.Select(k => k.Key).ToList(),
                Children = map.EntityChildren.Select(c => c.AliasTo).ToList()
            };
        }

        foreach (var map in maps)
        {
            foreach (var link in map.EntityChildren)
                AddEdge(graph, map.Alias, link.AliasTo, link.FromColumn, link.ToColumn);

            foreach (var link in map.ModelChildren)
                AddEdge(graph, map.Alias, link.AliasTo, link.FromColumn, link.ToColumn);
        }

        return graph;
    }

    private static void AddEdge(
        GraphIL graph,
        string from,
        string to,
        string fromCol,
        string toCol)
    {
        var edge = new GraphILEdge
        {
            FromAlias = from,
            ToAlias = to,
            FromColumn = fromCol,
            ToColumn = toCol
        };

        if (!graph.EdgesBySourceAlias.TryGetValue(from, out var list))
            graph.EdgesBySourceAlias[from] = list = new();

        list.Add(edge);

        if (!graph.EdgesByTargetAlias.TryGetValue(to, out var tlist))
            graph.EdgesByTargetAlias[to] = tlist = new();

        tlist.Add(edge);
    }
}