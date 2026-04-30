using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Helper;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public static class SqlNodeBuilder
    {
        public static void BuildFromMappings()
        {
            foreach (var (modelName, map) in MappingRegistry.GetAll())
            {
                BuildTree(modelName, map);
            }
            
            foreach (var (modelName, map) in MappingRegistry.GetAll())
            {
                BuildModel(modelName, map);
            }
        }
        
        public static void BuildModel(string model, NodeMap map)
        {
            var modelName = map.ModelType.Name;
            var table = map.EntityType?.Name;

            if (string.IsNullOrWhiteSpace(table))
            {
                
            }
            
            var linkKeys  = map.LinkKeys;
            
            var alias = map.Alias.Split('.').Last();

            if (!map.IsEntity)
            {
                alias = modelName;
            }
            else if (map.IsEntity && string.IsNullOrWhiteSpace(alias))
            {
                alias = SqlNodeRegistry.EntityTrees.ContainsKey(alias)
                    ? alias
                    : map.EntityType == null
                        ? map.ModelType.Name
                        : map.EntityType.Name;
            }
            
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
                    FromEnumeration = map.FromEnum,
                    ToEnumeration   = map.ToEnum,
                    EntityChildren = map.EntityChildren,
                    EntityParents = map.EntityParents,
                    EntityRelatedChildren = map.EntityRelatedChildren,
                    EntityRelatedParents = map.EntityRelatedParents,
                    UpsertKeys      = SqlNodeRegistry.EntityTrees.ContainsKey(alias)
                        ? SqlNodeRegistry.EntityTrees[alias].UpsertKeys.Select(b => $"{field.DestinationEntity}~{b}").ToList()
                        : map.EntityType != null && SqlNodeRegistry.EntityTrees.ContainsKey(map.EntityType.Name)
                            ? SqlNodeRegistry.EntityTrees[map.EntityType.Name].UpsertKeys.Select(b => $"{field.DestinationEntity}~{b}").ToList()
                            : new List<string>()
                };
                tempSqlNode.LinkKeys.AddRange(linkKeys);
                tempSqlNode.SqlNodeTypes ??= new List<SqlNodeType>();
                
                if (map.IsGraph)
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Graph);
                    
                tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Edge);
                tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Node);
                    
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
                    FromEnumeration = map.FromEnum,
                    ToEnumeration   = map.ToEnum,
                    EntityChildren = map.EntityChildren,
                    EntityParents = map.EntityParents,
                    EntityRelatedChildren = map.EntityRelatedChildren,
                    EntityRelatedParents = map.EntityRelatedParents,
                    UpsertKeys      = SqlNodeRegistry.EntityTrees.ContainsKey(alias)
                        ? SqlNodeRegistry.EntityTrees[alias].UpsertKeys.Select(b => $"{field.DestinationEntity}~{b}").ToList()
                        : map.EntityType != null && SqlNodeRegistry.EntityTrees.ContainsKey(map.EntityType.Name)
                            ? SqlNodeRegistry.EntityTrees[map.EntityType.Name].UpsertKeys.Select(b => $"{field.DestinationEntity}~{b}").ToList()
                            : new List<string>()
                };
                tempSqlNode.LinkKeys.AddRange(linkKeys);
                tempSqlNode.SqlNodeTypes ??= new List<SqlNodeType>();
                
                if (map.IsGraph)
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Graph);
                    
                tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Edge);
                tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Node);
                    
                SqlNodeRegistry.RegisterNode(
                    $"{alias}~{map.ModelType.Name}~{field.SourceName}", 
                    $"{alias}~{map.ModelType.Name}~{field.SourceName}",
                    tempSqlNode, map.ModelType,
                    map.EntityType == null ? map.ModelType : map.EntityType,
                    map.IsEntity);
                
                for (int i = 0; i < map.UpsertKeys.Count - 1; i++)
                {
                    tempSqlNode = new SqlNode
                    {
                        Id              = map.Id.ToString(),
                        Schema          = map.Schema,
                        Table           = (table ?? map.EntityType?.Name) ?? string.Empty,
                        Column          = map.UpsertKeys[i].Key,
                        RelationshipKey = $"{alias}~{map.ModelType.Name}~{map.UpsertKeys[i].Key}",
                        FromEnumeration = map.FromEnum,
                        ToEnumeration   = map.ToEnum,
                        EntityChildren = map.EntityChildren,
                        EntityParents = map.EntityParents,
                        EntityRelatedChildren = map.EntityRelatedChildren,
                        EntityRelatedParents = map.EntityRelatedParents,
                        UpsertKeys      = SqlNodeRegistry.EntityTrees.ContainsKey(alias)
                            ? SqlNodeRegistry.EntityTrees[alias].UpsertKeys.Select(b => $"{map.EntityType.Name}~{b}").ToList()
                            : map.EntityType != null && SqlNodeRegistry.EntityTrees.ContainsKey(map.EntityType.Name)
                                ? SqlNodeRegistry.EntityTrees[map.EntityType.Name].UpsertKeys.Select(b => $"{map.EntityType.Name}~{b}").ToList()
                                : new List<string>()
                    };
                    tempSqlNode.LinkKeys.AddRange(linkKeys);
                    tempSqlNode.SqlNodeTypes ??= new List<SqlNodeType>();
                
                    if (map.IsGraph)
                        tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Graph);
                    
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Edge);
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Node);
                
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
                        FromEnumeration = map.FromEnum,
                        ToEnumeration   = map.ToEnum,
                        EntityChildren = map.EntityChildren,
                        EntityParents = map.EntityParents,
                        EntityRelatedChildren = map.EntityRelatedChildren,
                        EntityRelatedParents = map.EntityRelatedParents,
                        UpsertKeys      = SqlNodeRegistry.EntityTrees.ContainsKey(alias)
                            ? SqlNodeRegistry.EntityTrees[alias].UpsertKeys.Select(b => $"{map.EntityType.Name}~{b}").ToList()
                            : map.EntityType != null && SqlNodeRegistry.EntityTrees.ContainsKey(map.EntityType.Name)
                                ? SqlNodeRegistry.EntityTrees[map.EntityType.Name].UpsertKeys.Select(b => $"{map.EntityType.Name}~{b}").ToList()
                                : new List<string>()
                    };
                    tempSqlNode.LinkKeys.AddRange(linkKeys);
                    tempSqlNode.SqlNodeTypes ??= new List<SqlNodeType>();
                
                    if (map.IsGraph)
                        tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Graph);
                    
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Edge);
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Node);
                
                    SqlNodeRegistry.RegisterNode(
                        $"{alias}~{map.ModelType.Name}~{map.UpsertKeys[i].Key}", 
                        $"{alias}~{map.ModelType.Name}~{map.UpsertKeys[i].Key}",
                        tempSqlNode, map.ModelType,
                        map.EntityType == null ? map.ModelType : map.EntityType,
                        map.IsEntity);
                }
            }

            // ── Enum Mapping ─────────────────────────────────────────────────
            if (map.FromEnum != null && map.ToEnum != null)
            {
                for (int i = 0; i < map.FromEnum.Count - 1; i++)
                {
                    var fromKeyParts = map.FromEnum.ElementAt(i).Key.Split('~');
                    var toKeyParts   = map.ToEnum.ElementAt(i).Key.Split('~');

                    var tempSqlNode = new SqlNode
                    {
                        Id              = map.Id.ToString(),
                        Schema          = map.Schema,
                        Table           = fromKeyParts[0],
                        Column          = fromKeyParts[2],
                        RelationshipKey = $"{alias}~{toKeyParts[1]}~{toKeyParts[2]}",
                        FromEnumeration = map.FromEnum,
                        ToEnumeration   = map.ToEnum,
                        EntityChildren = map.EntityChildren,
                        EntityParents = map.EntityParents,
                        EntityRelatedChildren = map.EntityRelatedChildren,
                        EntityRelatedParents = map.EntityRelatedParents,
                        UpsertKeys      = SqlNodeRegistry.EntityTrees.ContainsKey(alias)
                            ? SqlNodeRegistry.EntityTrees[alias].UpsertKeys.Select(b => $"{toKeyParts[0]}~{b}").ToList()
                            : map.EntityType != null && SqlNodeRegistry.EntityTrees.ContainsKey(map.EntityType.Name)
                                ? SqlNodeRegistry.EntityTrees[map.EntityType.Name].UpsertKeys.Select(b => $"{toKeyParts[0]}~{b}").ToList()
                                : new List<string>()
                    };
                    tempSqlNode.LinkKeys.AddRange(linkKeys);
                    tempSqlNode.SqlNodeTypes ??= new List<SqlNodeType>();

                    if (map.IsGraph)
                        tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Graph);
                    
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Edge);
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Node);
                    
                    SqlNodeRegistry.RegisterNode(
                        $"{alias}~{toKeyParts[1]}~{toKeyParts[2]}",
                        $"{alias}~{toKeyParts[1]}~{toKeyParts[2]}",
                        tempSqlNode, map.ModelType,
                        map.EntityType == null ? map.ModelType : map.EntityType,
                        map.IsEntity);
                    
                    tempSqlNode = new SqlNode
                    {
                        Id              = map.Id.ToString(),
                        Schema          = map.Schema,
                        Table           = fromKeyParts[0],
                        Column          = fromKeyParts[2],
                        RelationshipKey = $"{alias}~{toKeyParts[1]}~{toKeyParts[2]}",
                        FromEnumeration = map.FromEnum,
                        ToEnumeration   = map.ToEnum,
                        EntityChildren = map.EntityChildren,
                        EntityParents = map.EntityParents,
                        EntityRelatedChildren = map.EntityRelatedChildren,
                        EntityRelatedParents = map.EntityRelatedParents,
                        UpsertKeys      = SqlNodeRegistry.EntityTrees.ContainsKey(alias)
                            ? SqlNodeRegistry.EntityTrees[alias].UpsertKeys.Select(b => $"{toKeyParts[0]}~{b}").ToList()
                            : map.EntityType != null && SqlNodeRegistry.EntityTrees.ContainsKey(map.EntityType.Name)
                                ? SqlNodeRegistry.EntityTrees[map.EntityType.Name].UpsertKeys.Select(b => $"{toKeyParts[0]}~{b}").ToList()
                                : new List<string>()
                    };
                    tempSqlNode.LinkKeys.AddRange(linkKeys);
                    tempSqlNode.SqlNodeTypes ??= new List<SqlNodeType>();

                    if (map.IsGraph)
                        tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Graph);
                    
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Edge);
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Node);
                    
                    SqlNodeRegistry.RegisterNode(
                        $"{alias}~{toKeyParts[1]}~{toKeyParts[2]}",
                        $"{alias}~{toKeyParts[1]}~{toKeyParts[2]}",
                        tempSqlNode, map.ModelType,
                        map.EntityType == null ? map.ModelType : map.EntityType,
                        map.IsEntity);
                }
            }
        }
        
        public static void BuildTree(string modelName, NodeMap map)
        {
            var modelNameAux = modelName.Split('.').Length > 1
                ? modelName.Split('.')[1]
                : modelName;

            if (SqlNodeRegistry.EntityTrees.ContainsKey(modelNameAux))
                return;

            if (map.IsModel)
            {
                SqlNodeRegistry.ModelTrees[modelNameAux] = new NodeTree
                {
                    Id                 = map.Id,
                    Alias              = modelNameAux,
                    Name               = map.ModelType.Name,
                    Children           = map.EntityChildren,
                    Parents            = map.EntityParents,
                    RelatedParents     = map.EntityRelatedParents,
                    NodeMap = map,
                    RelatedChildren    = map.EntityRelatedChildren,
                    ModelToEntityLinks = map.ModelToEntityLinks,
                    Mapping            = map.FieldMaps.Select(a => new FieldMap
                    {
                        DestinationEntity = a.DestinationEntity,
                        DestinationName = a.DestinationName,
                        SourceModel = map.ModelType.Name,
                        SourceName = a.SourceName
                    }).ToList(),
                    IsGraph            = map.IsGraph,
                    IsEntity          = map.IsEntity,
                    IsModel          = map.IsModel,
                    UpsertKeys         = map.UpsertKeys.Select(b => b.Key).ToList()
                };
            }

            if (map.IsEntity)
            {
                SqlNodeRegistry.EntityTrees[modelNameAux] = new NodeTree
                {
                    Id                 = map.Id,
                    Alias              = modelNameAux,
                    Name               = map.EntityType.Name,
                    Schema             = map.Schema,
                    Children           = map.EntityChildren,
                    Parents            = map.EntityParents,
                    RelatedParents     = map.EntityRelatedParents,
                    NodeMap = map,
                    RelatedChildren    = map.EntityRelatedChildren,
                    ModelToEntityLinks = map.ModelToEntityLinks,
                    Mapping            = map.FieldMaps.Select(a => new FieldMap
                    {
                        DestinationEntity = a.DestinationEntity,
                        DestinationName = a.DestinationName,
                        SourceModel = map.ModelType.Name,
                        SourceName = a.SourceName
                    }).ToList(),
                    IsGraph            = map.IsGraph,
                    IsEntity          = map.IsEntity,
                    IsModel          = map.IsModel,
                    UpsertKeys         = map.UpsertKeys.Select(b => b.Key).ToList()
                };
            }
            
            if (map.IsGraph)
            {
                SqlNodeRegistry.EntityTrees[modelNameAux] = new NodeTree
                {
                    Id                 = map.Id,
                    Alias              = modelNameAux,
                    Name               = map.EntityType.Name,
                    Schema             = map.Schema,
                    Children           = map.EntityChildren,
                    Parents            = map.EntityParents,
                    RelatedParents     = map.EntityRelatedParents,
                    RelatedChildren    = map.EntityRelatedChildren,
                    ModelToEntityLinks = map.ModelToEntityLinks,
                    Mapping            = map.FieldMaps,
                    IsGraph            = map.IsGraph,
                    UpsertKeys         = map.UpsertKeys.Select(b => b.Key).ToList()
                };
                
                SqlNodeRegistry.ModelTrees[modelNameAux] = new NodeTree
                {
                    Id                 = map.Id,
                    Alias              = modelNameAux,
                    Name               = map.ModelType.Name,
                    Children           = map.EntityChildren,
                    Parents            = map.EntityParents,
                    RelatedParents     = map.EntityRelatedParents,
                    RelatedChildren    = map.EntityRelatedChildren,
                    ModelToEntityLinks = map.ModelToEntityLinks,
                    Mapping            = map.FieldMaps,
                    IsGraph            = map.IsGraph,
                    UpsertKeys         = map.UpsertKeys.Select(b => b.Key).ToList()
                };
            }
        }
    }
}