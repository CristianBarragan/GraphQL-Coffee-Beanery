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

        BuildRoleAliasClones(graph, all);

        foreach (var (_, map) in all)
        {
            BuildEdges(graph, map);
            RegisterFieldLookups(map);
        }

        return graph;
    }
    
    private void BuildRoleAliasClones(Graph graph, IReadOnlyDictionary<string, NodeMap> allMaps)
     {
         foreach (var (_, map) in allMaps)
         {
             foreach (var link in map.ModelToEntity)
             {
                 var resolvedAlias = $"{link.AliasProperty}{link.EntityType.Name}";

                 if (NodeRegistry.EntityTrees.ContainsKey(resolvedAlias))
                     continue;

                 var canonicalAlias = link.EntityType.Name;

                 if (!NodeRegistry.EntityTrees.TryGetValue(canonicalAlias, out var canonical))
                     continue;

                 graph.Nodes.TryAdd(resolvedAlias, new GraphNode
                 {
                     Alias = resolvedAlias,
                     Name = canonical.Name,
                 });

                 NodeRegistry.EntityTrees.TryAdd(resolvedAlias, new EntityNodeTree
                 {
                     Alias = resolvedAlias,
                     ModelName = canonical.ModelName,
                     Name = canonical.Name,
                     Schema = canonical.Schema,
                     Prefix = canonical.Prefix,
                     NodeMap = canonical.NodeMap,
                     EntityType = canonical.EntityType,
                     ModelType = canonical.ModelType,
                     IsGraph = canonical.IsGraph,
                     IsEntity = canonical.IsEntity,
                     IsModel = canonical.IsModel,
                     GraphMap = canonical.GraphMap,
                     EntityChildren = canonical.EntityChildren,
                     EntityChildrenRelated = canonical.EntityChildrenRelated,
                     ModelToEntity = canonical.ModelToEntity,
                     Mapping = canonical.Mapping,
                     UpsertKeys = canonical.UpsertKeys
                 });
             }
         }
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
             var primaryLink = map.ModelToEntity.FirstOrDefault(l => l.EntityType == map.EntityType);

             var canonicalAlias = primaryLink != null
                 ? $"{primaryLink.AliasProperty}{primaryLink.EntityType.Name}"
                 : map.EntityType.Name;

             graph.Nodes.TryAdd(canonicalAlias, new GraphNode
             {
                 Alias = canonicalAlias,
                 Name = map.EntityType.Name,
             });

             NodeRegistry.EntityTrees.TryAdd(canonicalAlias, new EntityNodeTree
             {
                 Alias = canonicalAlias,
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
                 ToAlias = map.Alias,
                 FieldName = field.SourceName,
                 Kind = GraphEdgeKind.ScalarField
             });
         }

         var seenModelToEntityKeys = new HashSet<(string FromAlias, string FieldName)>();

         foreach (var link in map.ModelToEntity)
         {
             var key = (map.Alias, link.AliasProperty);

             if (!seenModelToEntityKeys.Add(key))
                 continue;

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
    
    private void GenerateReflectedFieldMaps(NodeMap map)
    {
        if (map.ModelType is null || map.EntityType is null)
            return;

        if (map.ModelProperties.Count == 0)
            foreach (var p in map.ModelType.GetProperties())
                map.ModelProperties[p.Name] = p;

        if (map.EntityProperties.Count == 0)
            foreach (var p in map.EntityType.GetProperties())
                map.EntityProperties[p.Name] = p;

        var alreadyMapped = new HashSet<string>(
            map.FieldMaps.Select(f => f.SourceName),
            StringComparer.OrdinalIgnoreCase);

        var excluded = new HashSet<string>(
            map.ExcludedFieldMappings.Select(f => f.SourceName),
            StringComparer.OrdinalIgnoreCase);

        var fkSourceColumns = new HashSet<string>(
            map.ModelToEntity.Select(k => k.FromColumn),
            StringComparer.OrdinalIgnoreCase);

        foreach (var (name, _) in map.ModelProperties)
        {
            if (alreadyMapped.Contains(name) || excluded.Contains(name) || fkSourceColumns.Contains(name))
                continue;

            if (!map.EntityProperties.TryGetValue(name, out var entityProp))
                continue; // no identically-named column on the primary entity - nothing to reflect

            map.FieldMaps.Add(new FieldMap
            {
                SourceName = name,
                DestinationEntity = map.EntityType.Name,
                DestinationName = entityProp.Name
            });
        }
    }

    private void ResolveFieldMapAliases(NodeMap map)
    {
        foreach (var field in map.FieldMaps)
        {
            field.SourceAlias = map.Alias;

            if (string.IsNullOrEmpty(field.DestinationEntity))
            {
                field.DestinationAlias = map.Alias;
                continue;
            }

            var link = map.ModelToEntity
                .FirstOrDefault(l => l.EntityType.Name == field.DestinationEntity);

            field.DestinationAlias = link != null
                ? $"{link.AliasProperty}{link.EntityType.Name}"
                : field.DestinationEntity;
        }
    }

    private void RegisterFieldLookups(NodeMap map)
    {
        foreach (var field in map.FieldMaps)
        {
            var key = (field.SourceAlias, field.SourceName);

            if (!NodeRegistry.ColumnByField.TryGetValue(key, out var cols))
            {
                cols = new List<(string EntityAlias, string EntityColumn)>();
                NodeRegistry.ColumnByField[key] = cols;
            }

            cols.Add((field.DestinationAlias, field.DestinationName));

            if (field.ToEnum.Count > 0)
                NodeRegistry.EnumByField[key] = field.ToEnum;
        }

        foreach (var link in map.ModelToEntity)
        {
            var key = (map.Alias, link.AliasProperty);
            NodeRegistry.ChildAliasByField[key] = $"{link.AliasProperty}{link.EntityType.Name}";
        }
    }

    public void BuildFromMappings()
    {
        var graph = Build();
        NodeRegistry.Register(graph);
        NodeRegistry.Freeze();
    }

    // InferModelChildren left as a no-op - see note below.
    private void InferModelChildren(NodeMap map) { }
}