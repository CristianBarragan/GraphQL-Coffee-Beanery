using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class GraphILIndexBuilder
{
    public static GraphIL Build()
    {
        var all = MappingRegistry.GetAll();

        var nodes = new Dictionary<string, GraphILNode>(StringComparer.OrdinalIgnoreCase);

        var edgesBySource = new Dictionary<string, List<GraphILEdge>>(StringComparer.OrdinalIgnoreCase);
        var edgesByTarget = new Dictionary<string, List<GraphILEdge>>(StringComparer.OrdinalIgnoreCase);

        // -------------------------
        // 1. Build nodes only
        // -------------------------
        foreach (var (_, map) in all)
        {
            if (!nodes.TryGetValue(map.Alias, out var node))
            {
                node = new GraphILNode
                {
                    Alias = map.Alias,
                    EntityType = map.EntityType,
                    TableName = map.EntityType?.Name ?? map.ModelType.Name,
                    UpsertKeys = map.UpsertKeys.Select(k => k.Key).ToList(),
                    Fields = new List<GraphILField>()
                };

                nodes[map.Alias] = node;
            }

            foreach (var field in map.FieldMaps)
            {
                node.Fields.Add(new GraphILField
                {
                    SourceName = field.SourceName,
                    DestinationName = field.DestinationName
                });
            }
        }

        // -------------------------
        // 2. Build edges (ONLY source of truth)
        // -------------------------
        foreach (var (_, map) in all)
        {
            AddEdges(map, map.EntityChildren);
            AddEdges(map, map.EntityRelatedChildren);
        }

        return new GraphIL
        {
            SchemaVersion = "v1",
            Nodes = nodes,
            EdgesBySourceAlias = edgesBySource,
            EdgesByTargetAlias = edgesByTarget
        };

        // -------------------------
        // local helper
        // -------------------------
        void AddEdges(NodeMap map, IEnumerable<LinkKey> links)
        {
            foreach (var link in links ?? Enumerable.Empty<LinkKey>())
            {
                var edge = new GraphILEdge
                {
                    FromAlias = map.Alias,
                    ToAlias = link.AliasTo,
                    FromColumn = link.FromColumn,
                    ToColumn = link.ToColumn
                };

                if (!edgesBySource.TryGetValue(edge.FromAlias, out var list))
                    edgesBySource[edge.FromAlias] = list = new List<GraphILEdge>();

                list.Add(edge);

                if (!edgesByTarget.TryGetValue(edge.ToAlias, out var tlist))
                    edgesByTarget[edge.ToAlias] = tlist = new List<GraphILEdge>();

                tlist.Add(edge);
            }
        }
    }
}