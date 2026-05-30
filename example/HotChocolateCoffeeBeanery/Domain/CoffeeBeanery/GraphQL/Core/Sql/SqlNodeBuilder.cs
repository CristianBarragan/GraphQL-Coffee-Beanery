using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Helper;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public static class SqlNodeBuilder
    {
        public static void BuildFromMappings()
        {
            var all = MappingRegistry.GetAll();
            Console.WriteLine($"Registry count: {all.Count}");
            foreach (var kvp in all)
                Console.WriteLine($"  [{kvp.Key}] IsModel={kvp.Value.IsModel} IsEntity={kvp.Value.IsEntity}");

            foreach (var (modelName, map) in all)
                BuildTree(modelName, map);

            foreach (var (modelName, map) in all)
                BuildModel(modelName, map);
        }
        
        public static void BuildModel(string model, NodeMap map)
        {
            var modelName = map.ModelType.Name;
            var table = map.EntityType?.Name;

            if (string.IsNullOrWhiteSpace(table))
            {
                
            }
            
            var linkKeys  = map.LinkKeys;
            var alias = model;           
            
            // var alias = map.Alias.Split('.').Last();

            // if (!map.IsEntity)
            // {
            //     alias = modelName;
            // }
            // else if (map.IsEntity && string.IsNullOrWhiteSpace(alias))
            // {
            //     alias = SqlNodeRegistry.EntityTrees.ContainsKey(alias)
            //         ? alias
            //         : map.EntityType == null
            //             ? map.ModelType.Name
            //             : map.EntityType.Name;
            // }
            
            foreach (var fieldMap in map.FieldMaps)
            {
                linkKeys.Add(new LinkKey
                {
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
                    Schema          = map.Schema,
                    Table           = (table ?? map.EntityType?.Name) ?? string.Empty,
                    Column          = field.DestinationName,
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
                    Schema          = map.Schema,
                    Table           = (table ?? map.EntityType?.Name) ?? string.Empty,
                    Column          = field.DestinationName,
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
                
                for (int i = 0; i < map.UpsertKeys.Count; i++)
                {
                    tempSqlNode = new SqlNode
                    {
                        Id              = map.Id.ToString(),
                        Schema          = map.Schema,
                        Table           = (table ?? map.EntityType?.Name) ?? string.Empty,
                        Column          = map.UpsertKeys[i].Key,
                        RelationshipKey = $"{alias}~{map.ModelType.Name}~{map.UpsertKeys[i].Key}",
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
                        $"{alias}~{map.ModelType.Name}~{map.UpsertKeys[i].Key}", 
                        $"{alias}~{map.ModelType.Name}~{map.UpsertKeys[i].Key}",
                        tempSqlNode, map.ModelType,
                        map.EntityType == null ? map.ModelType : map.EntityType,
                        map.IsEntity);
                    
                    tempSqlNode = new SqlNode
                    {
                        Id              = map.Id.ToString(),
                        Schema          = map.Schema,
                        Table           = (table ?? map.EntityType?.Name) ?? string.Empty,
                        Column          = map.UpsertKeys[i].Key,
                        RelationshipKey = $"{alias}~{map.ModelType.Name}~{map.UpsertKeys[i].Key}",
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
                        $"{alias}~{map.ModelType.Name}~{map.UpsertKeys[i].Key}", 
                        $"{alias}~{map.ModelType.Name}~{map.UpsertKeys[i].Key}",
                        tempSqlNode, map.ModelType,
                        map.EntityType == null ? map.ModelType : map.EntityType,
                        map.IsEntity);
                }
            }
        }
        
        public static void BuildTree(string modelName, NodeMap map)
        {
            // FIXED: guard ModelTree and EntityTree independently
            // instead of bailing out entirely when EntityTree already exists
            var modelTreeMissing  = !SqlNodeRegistry.ModelTrees.ContainsKey(modelName);
            var entityTreeMissing = !SqlNodeRegistry.EntityTrees.ContainsKey(modelName);

            // Nothing to do at all
            if (!modelTreeMissing && !entityTreeMissing)
                return;

            if (map.IsModel && modelTreeMissing)
            {
                SqlNodeRegistry.ModelTrees[modelName] = new NodeTree
                {
                    Id                 = map.Id,
                    Alias              = modelName,
                    Name               = map.ModelType.Name,
                    EntityType         = map.EntityType,
                    ModelType          = map.ModelType,
                    Children           = map.EntityChildren,
                    Parents            = map.EntityParents,
                    RelatedParents     = map.EntityRelatedParents,
                    NodeMap            = map,
                    RelatedChildren    = map.EntityRelatedChildren,
                    ModelToEntityLinks = map.ModelToEntityLinks,
                    Mapping            = map.FieldMaps.Select(a => new FieldMap
                    {
                        DestinationEntity = a.DestinationEntity,
                        DestinationName   = a.DestinationName,
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
                SqlNodeRegistry.EntityTrees[modelName] = new NodeTree
                {
                    Id                 = map.Id,
                    Alias              = modelName,
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
                        DestinationEntity = a.DestinationEntity,
                        DestinationName   = a.DestinationName,
                        SourceModel       = map.ModelType.Name,
                        SourceName        = a.SourceName
                    }).ToList(),
                    IsGraph    = map.IsGraph,
                    IsEntity   = map.IsEntity,
                    IsModel    = map.IsModel,
                    UpsertKeys      = map.UpsertKeys.Select(a => $"{a.Entity}~{a.Key}").ToList()
                };
            }

            if (map.IsGraph)
            {
                if (entityTreeMissing)
                {
                    SqlNodeRegistry.EntityTrees[modelName] = new NodeTree
                    {
                        Id                 = map.Id,
                        Alias              = modelName,
                        Name               = map.EntityType.Name,
                        Schema             = map.Schema,
                        Children           = map.EntityChildren,
                        Parents            = map.EntityParents,
                        RelatedParents     = map.EntityRelatedParents,
                        RelatedChildren    = map.EntityRelatedChildren,
                        ModelToEntityLinks = map.ModelToEntityLinks,
                        Mapping            = map.FieldMaps,
                        IsGraph            = map.IsGraph,
                        UpsertKeys      = map.UpsertKeys.Select(a => $"{a.Entity}~{a.Key}").ToList()
                    };
                }

                if (modelTreeMissing)
                {
                    SqlNodeRegistry.ModelTrees[modelName] = new NodeTree
                    {
                        Id                 = map.Id,
                        Alias              = modelName,
                        Name               = map.ModelType.Name,
                        Children           = map.EntityChildren,
                        Parents            = map.EntityParents,
                        RelatedParents     = map.EntityRelatedParents,
                        RelatedChildren    = map.EntityRelatedChildren,
                        ModelToEntityLinks = map.ModelToEntityLinks,
                        Mapping            = map.FieldMaps,
                        IsGraph            = map.IsGraph,
                        UpsertKeys      = map.UpsertKeys.Select(a => $"{a.Entity}~{a.Key}").ToList()
                    };
                }
            }
        }
    }
}