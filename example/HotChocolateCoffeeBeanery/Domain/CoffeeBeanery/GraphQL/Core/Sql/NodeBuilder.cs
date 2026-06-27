using CoffeeBeanery.GraphQL.Core.Mapping;
using Microsoft.EntityFrameworkCore;

namespace CoffeeBeanery.GraphQL.Core.Sql;

public sealed class NodeBuilder<TContext>
    where TContext : DbContext
{
    private readonly EfEntityMetadata<TContext> _metadata;

    public NodeBuilder(EfEntityMetadata<TContext> metadata)
    {
        _metadata = metadata;
    }

    public Graph Build()
    {
        var graph = new Graph();
        var all = MappingRegistry.GetAll();

        foreach (var (_, map) in all)
            InferModelChildren(map);

        foreach (var (_, map) in all)
            GenerateReflectedFieldMaps(map);

        foreach (var (_, map) in all)
            ResolveFieldMapAliases(map);

        foreach (var (_, map) in all)
            BuildNodes(graph, map);

        foreach (var (_, map) in all)
            BuildEdges(graph, map);

        return graph;
    }

    private void BuildNodes(Graph graph, NodeMap map)
    {
        if (map.IsModel)
        {
            graph.Nodes.TryAdd(map.Alias, new GraphNode
            {
                Alias = map.Alias,
                Name = map.ModelType.Name,
            });

            NodeRegistry.ModelTrees.TryAdd(map.Alias, new ModelNodeTree
            {
                Alias = map.Alias,
                ModelName = map.ModelName ?? map.ModelType.Name,
                ModelType = map.ModelType,
                EntityType = map.IsEntity ? map.EntityType : null
            });
        }

        if (map.IsEntity)
        {
            graph.Nodes.TryAdd(map.Alias, new GraphNode
            {
                Alias = map.Alias,
                Name = map.EntityType.Name,
            });

            NodeRegistry.EntityTrees.TryAdd(map.Alias, new EntityNodeTree
            {
                Alias = map.Alias,
                ModelName = map.ModelName ?? map.ModelType?.Name ?? map.EntityType.Name,
                Name = map.EntityType.Name,
                Schema = map.Schema,
                Prefix = map.Prefix,
                NodeMap = map,
                EntityType = map.EntityType,
                ModelType = map.ModelType,
                IsGraph = map.IsGraph,
                IsEntity = map.IsEntity,
                IsModel = map.IsModel,
                GraphMap = map.GraphMap,
                EntityChildren = map.EntityChildren,
                EntityChildrenRelated = map.EntityChildrenRelated,
                ModelToEntity = map.ModelToEntity,
                Mapping = map.FieldMaps,
                UpsertKeys = map.UpsertKeys.Select(k => k.Key).ToList()
            });
        }
    }

    private void BuildEdges(Graph graph, NodeMap map)
    {
        foreach (var field in map.FieldMaps)
        {
            graph.Edges.Add(new GraphEdge
            {
                FromAlias = map.Alias,
                ToAlias = ResolveDestinationAlias(map, field),
                FieldName = field.SourceName,
                Kind = GraphEdgeKind.ScalarField
            });
        }

        var seenModelToEntityKeys = new HashSet<(string FromAlias, string FieldName)>();

        foreach (var link in map.ModelToEntity)
        {
            var key = (map.Alias, link.AliasProperty);

            if (!seenModelToEntityKeys.Add(key))
                continue; // duplicate link for the same (model alias, role) - same relationship, registered twice upstream

            graph.Edges.Add(new GraphEdge
            {
                FromAlias = map.Alias,
                ToAlias = $"{link.AliasProperty}{link.EntityType.Name}",
                FieldName = link.AliasProperty,
                Kind = GraphEdgeKind.ModelToEntity,
                FromColumn = link.FromColumn,
                ToColumn = link.ToColumn
            });
        }
    }

    private string ResolveDestinationAlias(NodeMap map, FieldMap field)
    {
        var key = (map.Alias, field.SourceName);
        if (NodeRegistry.ChildAliasByField.TryGetValue(key, out var alias))
            return alias;

        return map.Alias;
    }

    public void BuildFromMappings()
    {
        var graph = Build();
        NodeRegistry.Register(graph);
        NodeRegistry.Freeze();
    }

    private void InferModelChildren(NodeMap map) { }

    private void GenerateReflectedFieldMaps(NodeMap map) { }

    private void ResolveFieldMapAliases(NodeMap map) { }
}