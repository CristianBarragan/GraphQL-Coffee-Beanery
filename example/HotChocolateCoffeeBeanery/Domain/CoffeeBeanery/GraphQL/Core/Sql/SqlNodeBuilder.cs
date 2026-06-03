using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public static class SqlNodeBuilder
    {
        public static void BuildFromMappings()
        {
            var all = MappingRegistry.GetAll();
            
            foreach (var (modelName, map) in all)
                BuildTree(modelName, map);

            foreach (var (modelName, map) in all)
                BuildModel(modelName, map);
        }
        
        public static void BuildModel(string model, NodeMap map)
        {
            var modelName = map.ModelType.Name;
            
            var table = map.EntityType?.Name;
            var linkKeys  = map.LinkKeys;
            var alias = model;
            
            foreach (var fieldMap in map.FieldMaps)
            {
                linkKeys.Add(new LinkKey
                {
                    AliasFrom = fieldMap.SourceAlias,
                    AliasTo = fieldMap.DestinationAlias,
                    From       = modelName,
                    FromColumn = fieldMap.SourceName,
                    To         = fieldMap.DestinationEntity,
                    ToColumn   = fieldMap.DestinationName
                });
            }

            // ── Fields ───────────────────────────────────────────────────────
            foreach (var field in map.FieldMaps)
            {
                var tempSqlNode = new SqlNode
                {
                    Id              = map.Id.ToString(),
                    Alias = alias,
                    Schema          = map.Schema,
                    Table           = (table ?? map.EntityType?.Name) ?? string.Empty,
                    Column          = field.DestinationName,
                    SourceColumn          = field.SourceName,
                    RelationshipKey = $"{alias}~{map.ModelType.Name}~{field.SourceName}",
                    FromEnumeration = field.FromEnum,
                    ToEnumeration   = field.ToEnum,
                    EntityChildren = map.EntityChildren,
                    EntityParents = map.EntityParents,
                    EntityRelatedChildren = map.EntityRelatedChildren,
                    EntityRelatedParents = map.EntityRelatedParents,
                    UpsertKeys      = map.UpsertKeys.Select(a => $"{a.Entity}~{a.Key}").ToList()
                };
                tempSqlNode.LinkKeys.AddRange(linkKeys);
                tempSqlNode.SqlNodeTypes ??= new List<SqlNodeType>();
                
                if (map.IsGraph)
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Graph);
                    
                SqlNodeRegistry.RegisterNode(
                    $"{alias}~{map.ModelType.Name}~{field.SourceName}", 
                    $"{alias}~{map.ModelType.Name}~{field.SourceName}",
                    tempSqlNode, map.ModelType,
                    map.EntityType == null ? map.ModelType : map.EntityType,
                    map.IsEntity);
                
                alias = map.Alias;
                
                tempSqlNode = new SqlNode
                {
                    Id              = map.Id.ToString(),
                    Alias = alias,
                    Schema          = map.Schema,
                    Table           = (table ?? map.EntityType?.Name) ?? string.Empty,
                    Column          = field.DestinationName,
                    SourceColumn          = field.SourceName,
                    RelationshipKey = $"{alias}~{map.ModelType.Name}~{field.SourceName}",
                    FromEnumeration = field.FromEnum,
                    ToEnumeration   = field.ToEnum,
                    EntityChildren = map.EntityChildren,
                    EntityParents = map.EntityParents,
                    EntityRelatedChildren = map.EntityRelatedChildren,
                    EntityRelatedParents = map.EntityRelatedParents,
                    UpsertKeys      = map.UpsertKeys.Select(a => $"{a.Entity}~{a.Key}").ToList()
                };
                tempSqlNode.LinkKeys.AddRange(linkKeys);
                tempSqlNode.SqlNodeTypes ??= new List<SqlNodeType>();
                
                if (map.IsGraph)
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Graph);
                    
                SqlNodeRegistry.RegisterNode(
                    $"{alias}~{map.ModelType.Name}~{field.SourceName}", 
                    $"{alias}~{map.ModelType.Name}~{field.SourceName}",
                    tempSqlNode, map.ModelType,
                    map.EntityType == null ? map.ModelType : map.EntityType,
                    map.IsEntity);
            }
        }
        
        public static void BuildTree(string alias, NodeMap map)
        {
            var modelTreeMissing  = !SqlNodeRegistry.ModelTrees.ContainsKey(alias);
            var entityTreeMissing = !SqlNodeRegistry.EntityTrees.ContainsKey(alias);

            if (!modelTreeMissing && !entityTreeMissing)
                return;

            if (map.IsModel && modelTreeMissing)
            {
                SqlNodeRegistry.ModelTrees[alias] = new NodeTree
                {
                    Id                 = map.Id,
                    ModelName = map.ModelName,
                    Alias              = alias,
                    Name               = map.ModelType.Name,
                    EntityType         = map.EntityType,
                    ModelType          = map.ModelType,
                    Schema             = map.Schema,
                    Children           = map.EntityChildren,
                    Parents            = map.EntityParents,
                    RelatedParents     = map.EntityRelatedParents,
                    NodeMap            = map,
                    RelatedChildren    = map.EntityRelatedChildren,
                    ModelToEntityLinks = map.ModelToEntityLinks,
                    Mapping            = map.FieldMaps.Select(a => new FieldMap
                    {
                        DestinationAlias = a.DestinationAlias,
                        DestinationEntity = a.DestinationEntity,
                        DestinationName   = a.DestinationName,
                        SourceAlias      = a.SourceAlias,
                        SourceModel       = map.ModelType.Name,
                        SourceName        = a.SourceName
                    }).ToList(),
                    IsGraph    = map.IsGraph,
                    IsEntity   = map.IsEntity,
                    IsModel    = map.IsModel,
                    UpsertKeys      = map.UpsertKeys.Select(a => $"{a.Entity}~{a.Key}").ToList()
                };
            }

            if (map.IsEntity && entityTreeMissing)
            {
                SqlNodeRegistry.EntityTrees[alias] = new NodeTree
                {
                    Id                 = map.Id,
                    ModelName = map.ModelName,
                    Alias              = alias,
                    Name               = map.EntityType.Name,
                    EntityType         = map.EntityType,
                    ModelType          = map.ModelType,
                    Schema             = map.Schema,
                    Children           = map.EntityChildren,
                    Parents            = map.EntityParents,
                    RelatedParents     = map.EntityRelatedParents,
                    NodeMap            = map,
                    RelatedChildren    = map.EntityRelatedChildren,
                    ModelToEntityLinks = map.ModelToEntityLinks,
                    Mapping            = map.FieldMaps.Select(a => new FieldMap
                    {
                        DestinationAlias = a.DestinationAlias,
                        DestinationEntity = a.DestinationEntity,
                        DestinationName   = a.DestinationName,
                        SourceAlias      = a.SourceAlias,
                        SourceModel       = map.ModelType.Name,
                        SourceName        = a.SourceName
                    }).ToList(),
                    IsGraph    = map.IsGraph,
                    IsEntity   = map.IsEntity,
                    IsModel    = map.IsModel,
                    UpsertKeys      = map.UpsertKeys.Select(a => $"{a.Entity}~{a.Key}").ToList()
                };
            }
        }
    }
}