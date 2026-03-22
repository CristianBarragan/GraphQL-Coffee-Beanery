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
            var linkKeys = map.LinkKeys;

            foreach (var fieldMap in map.FieldMaps)
            {
                linkKeys.Add(new LinkKey()
                {
                    From = modelName,
                    FromColumn = fieldMap.SourceName,
                    To = fieldMap.DestinationEntity,
                    ToColumn = fieldMap.DestinationEntity
                });
            }

            // -------------------------
            // Field
            // -------------------------
            foreach (var field in map.FieldMaps)
            {
                var tempSqlNode = new SqlNode
                {
                    Id = map.Id.ToString(),
                    Schema = map.Schema,
                    Table = field.DestinationEntity,
                    Column = field.DestinationName,
                    RelationshipKey = $"{map.Alias}~{map.ModelType.Name}~{field.DestinationName}",
                    FromEnumeration = map.FromEnum,
                    ToEnumeration = map.ToEnum,
                    UpsertKeys = SqlNodeRegistry.EntityTrees[field.DestinationEntity].UpsertKeys.Select(b => $"{field.DestinationEntity}~{b}").ToList()
                };
                tempSqlNode.LinkKeys.AddRange(linkKeys);
                tempSqlNode.SqlNodeTypes ??= new List<SqlNodeType>();
                
                if (map.IsGraph)
                {
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Graph);
                }
                    
                if (map.IsModel)
                {
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Edge);
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Node);
                }
                    
                if (map.IsEntity)
                {
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Edge);
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Node);
                }
                
                SqlNodeRegistry.RegisterNode($"{map.Alias}~{map.ModelType.Name}~{field.DestinationName}", $"{map.Alias}~{map.ModelType.Name}~{field.DestinationName}", tempSqlNode);
            }

            // -------------------------
            // Enum Mapping for Enums
            // -------------------------
            if (map.FromEnum != null && map.ToEnum != null)
            {
                for (int i = 0; i < map.FromEnum.Count - 1; i++)
                {
                    var fromKeyParts = map.FromEnum.ElementAt(i).Key.Split('~');
                    var toKeyParts = map.ToEnum.ElementAt(i).Key.Split('~');
                    
                    var tempSqlNode = new SqlNode
                    {
                        Id = map.Id.ToString(),
                        Schema = map.Schema,
                        Table = fromKeyParts[0],
                        Column = fromKeyParts[2],
                        RelationshipKey = $"{map.Alias}~{map.ModelType.Name}~{toKeyParts[2]}",
                        FromEnumeration = map.FromEnum,
                        ToEnumeration = map.ToEnum,
                        UpsertKeys = SqlNodeRegistry.EntityTrees[toKeyParts[0]].UpsertKeys.Select(b => $"{toKeyParts[0]}~{b}").ToList()
                    };
                    tempSqlNode.LinkKeys.AddRange(linkKeys);
                    tempSqlNode.SqlNodeTypes ??= new List<SqlNodeType>();

                    if (map.IsGraph)
                    {
                        tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Graph);
                    }
                    
                    if (map.IsModel)
                    {
                        tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Edge);
                        tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Node);
                    }
                    
                    if (map.IsEntity)
                    {
                        tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Edge);
                        tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Node);
                    }
                    
                    SqlNodeRegistry.RegisterNode($"{map.Alias}~{map.ModelType.Name}~{toKeyParts[2]}", $"{map.Alias}~{toKeyParts[1]}~{toKeyParts[2]}", tempSqlNode);
                }
            }
            
            // -------------------------
            // Mutations (Upsert keys)
            // -------------------------
            for (int i = 0; i < map.UpsertKeys.Count - 1; i++)
            {
                var tempSqlNode = new SqlNode
                {
                    Id = map.Id.ToString(),
                    Schema = map.Schema,
                    Table = map.EntityType.Name,
                    Column = map.UpsertKeys[i].Key,
                    RelationshipKey = $"{map.Alias}~{map.ModelType.Name}~{map.ToEnum.ElementAt(i).Key}",
                    FromEnumeration = map.FromEnum,
                    ToEnumeration = map.ToEnum,
                    UpsertKeys = SqlNodeRegistry.EntityTrees[map.EntityType.Name].UpsertKeys.Select(b => $"{map.EntityType.Name}~{b}").ToList()
                };
                tempSqlNode.LinkKeys.AddRange(linkKeys);
                
                if (map.IsGraph)
                {
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Graph);
                }
                    
                if (map.IsModel)
                {
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Edge);
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Node);
                }
                    
                if (map.IsEntity)
                {
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Edge);
                    tempSqlNode.SqlNodeTypes.Add(SqlNodeType.Node);
                }
                
                SqlNodeRegistry.RegisterNode($"{map.Alias}~{map.ModelType.Name}~{map.ToEnum.ElementAt(i).Key}", $"{map.Alias}~{map.EntityType.Name}~{map.ToEnum.ElementAt(i).Key}", tempSqlNode);
            }
        }
        
        public static void BuildTree(string modelName, NodeMap map)
        {
            // -------------------------
            // Trees
            // -------------------------

            if (map.IsModel)
            {
                SqlNodeRegistry.ModelTrees[modelName] = new NodeTree
                {
                    Id = map.Id,
                    Alias = modelName,
                    Name = map.ModelType.Name,
                    Children = map.Children,
                    Mapping = map.FieldMaps,
                    IsGraph = map.IsGraph,
                    UpsertKeys = map.UpsertKeys.Select(b => b.Key).ToList()
                };
                
                SqlNodeRegistry.ModelTypes.Add(map.ModelType);
                SqlNodeRegistry.ModelNames.Add(map.ModelType.Name);
            }

            if (map.IsEntity)
            {
                SqlNodeRegistry.EntityTrees[map.EntityType.Name] = new NodeTree
                {
                    Id = map.Id,
                    Alias = map.EntityType.Name,
                    Name = map.EntityType.Name,
                    Schema = map.Schema,
                    Children = map.Children,
                    Mapping = map.FieldMaps,
                    IsGraph = map.IsGraph,
                    UpsertKeys = map.UpsertKeys.Select(b => b.Key).ToList()
                };
                
                SqlNodeRegistry.EntityTypes.Add(map.EntityType);
                SqlNodeRegistry.EntityNames.Add(map.EntityType.Name);
            }
            
            if (map.IsGraph)
            {
                SqlNodeRegistry.EntityTrees[map.EntityType.Name] = new NodeTree
                {
                    Id = map.Id,
                    Alias = map.EntityType.Name,
                    Name = map.EntityType.Name,
                    Schema = map.Schema,
                    Children = map.Children,
                    Mapping = map.FieldMaps,
                    IsGraph = map.IsGraph,
                    UpsertKeys = map.UpsertKeys.Select(b => b.Key).ToList()
                };
                
                SqlNodeRegistry.EntityTypes.Add(map.EntityType);
                SqlNodeRegistry.EntityNames.Add(map.EntityType.Name);
                
                SqlNodeRegistry.ModelTrees[modelName] = new NodeTree
                {
                    Id = map.Id,
                    Alias = modelName,
                    Name = map.ModelType.Name,
                    Children = map.Children,
                    Mapping = map.FieldMaps,
                    IsGraph = map.IsGraph,
                    UpsertKeys = map.UpsertKeys.Select(b => b.Key).ToList()
                };
                
                SqlNodeRegistry.ModelTypes.Add(map.ModelType);
                SqlNodeRegistry.ModelNames.Add(map.ModelType.Name);
            }
        }
    }
}
