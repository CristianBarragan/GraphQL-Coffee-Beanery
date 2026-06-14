using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Runtime;

namespace CoffeeBeanery.GraphQL.Core.Sql;

public static class SqlNodeBuilder
{
    public static GraphIL Build()
    {
        var all = MappingRegistry.GetAll();

        var nodes        = new Dictionary<string, GraphILNode>(StringComparer.OrdinalIgnoreCase);
        var edgesBySource = new Dictionary<string, List<GraphILEdge>>(StringComparer.OrdinalIgnoreCase);
        var edgesByTarget = new Dictionary<string, List<GraphILEdge>>(StringComparer.OrdinalIgnoreCase);

        // -------------------------
        // 1. Build nodes
        // -------------------------
        foreach (var (_, map) in all)
        {
            if (!nodes.TryGetValue(map.Alias, out var node))
            {
                node = new GraphILNode
                {
                    Alias      = map.Alias,
                    EntityType = map.EntityType,
                    TableName  = map.EntityType?.Name ?? map.ModelType.Name,
                    Fields     = new List<GraphILField>(),
                    UpsertKeys = map.UpsertKeys.Select(k => k.Key).ToList(),
                    Schema     = map.Schema,
                    GraphMap   = map.GraphMap
                };

                nodes[map.Alias] = node;
            }
            
            if (!nodes.TryGetValue(map.Alias, out node))
            {
                node = new GraphILNode
                {
                    Alias      = map.Alias,
                    EntityType = map.EntityType,
                    TableName  = map.EntityType?.Name ?? map.ModelType.Name,
                    Fields     = new List<GraphILField>(),
                    UpsertKeys = map.UpsertKeys.Select(k => k.Key).ToList(),
                    Schema     = map.Schema,
                    IsModel    = map.IsModel,
                    IsEntity   = map.IsEntity 
                };

                nodes[map.Alias] = node;
            }

            foreach (var field in map.FieldMaps)
            {
                // Mirror GetMutations: FromEnumeration key = GraphQL enum string,
                // value = database representation (int, string, etc.)
                var enumerations = field.FromEnum?
                    .ToDictionary(
                        e => e.Key,
                        e => e.Value,
                        StringComparer.OrdinalIgnoreCase)
                    ?? new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                node.Fields.Add(new GraphILField
                {
                    SourceName      = field.SourceName,
                    DestinationName = field.DestinationName,
                    Enumerations    = enumerations
                });
            }
            
            node.Columns = node.Fields
                .Where(f => !string.IsNullOrWhiteSpace(f.DestinationName))
                .Select(f => f.DestinationName)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        // -------------------------
        // 2. Build edges
        // -------------------------
        foreach (var (_, map) in all)
        {
            AddEdges(map, map.EntityChildren);
            AddEdges(map, map.EntityRelatedChildren);
            AddEdges(map, map.ModelChildren);
        }

        return new GraphIL
        {
            SchemaVersion      = "v1",
            Nodes              = nodes,
            EdgesBySourceAlias = edgesBySource,
            EdgesByTargetAlias = edgesByTarget
        };

        void AddEdges(NodeMap map, IEnumerable<LinkKey> links)
        {
            foreach (var link in links ?? Enumerable.Empty<LinkKey>())
            {
                var edge = new GraphILEdge
                {
                    FromAlias  = map.Alias,
                    ToAlias    = link.AliasTo,
                    FromColumn = link.FromColumn,
                    ToColumn   = link.ToColumn
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