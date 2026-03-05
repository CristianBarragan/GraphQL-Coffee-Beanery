using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public static class SqlNodeBuilder
    {
        public static void BuildFromMappings()
        {
            foreach (var (modelName, map) in MappingRegistry.GetAll())
            {
                BuildModel(modelName, map);
            }
        }
        
        public static void BuildModel(string modelName, NodeMap map)
        {
            var entityName = map.FieldMaps.First().DestinationEntity;
            var linkKeys = map.LinkKeys;

            foreach (var fieldMap in map.FieldMaps)
            {
                linkKeys.Add(new LinkKey()
                {
                    From = modelName,
                    FromColumn = fieldMap.SourceName,
                    To = entityName,
                    ToColumn = fieldMap.DestinationEntity
                });
            }

            // -------------------------
            // Trees
            // -------------------------

            if (map.IsModel)
            {
                SqlNodeRegistry.ModelTrees[modelName] = new NodeTree
                {
                    Id = map.Id,
                    Name = modelName,
                    Children = map.Children,
                    Mapping = map.FieldMaps,
                    IsGraph = map.IsGraph
                };
                
                SqlNodeRegistry.ModelTypes.Add(map.ModelType);
            }

            if (map.IsEntity)
            {
                SqlNodeRegistry.EntityTrees[entityName] = new NodeTree
                {
                    Id = map.Id,
                    Name = entityName,
                    Schema = map.Schema,
                    Children = map.Children,
                    Mapping = map.FieldMaps,
                    IsGraph = map.IsGraph
                };
                
                SqlNodeRegistry.EntityTypes.Add(map.EntityType);
            }

            // -------------------------
            // Field nodes
            // -------------------------
            foreach (var field in map.FieldMaps)
            {
                var tempSqlNode = new SqlNode
                {
                    Id = map.Id.ToString(),
                    Schema = map.Schema,
                    Table = entityName,
                    Column = field.DestinationName,
                    RelationshipKey = $"{entityName}~{field.DestinationName}",
                    SqlNodeType = SqlNodeType.Node,
                    FromEnumeration = map.FromEnum,
                    ToEnumeration = map.ToEnum,
                    UpsertKeys = map.UpsertKeys.Select(x => $"{x.Entity}~{x.Key}").ToList(),
                };
                tempSqlNode.LinkKeys.AddRange(linkKeys);
                
                SqlNodeRegistry.RegisterNode($"{modelName}~{field.SourceName}", $"{entityName}~{field.DestinationName}", tempSqlNode);
            }

            // -------------------------
            // Enum Mapping for Enums
            // -------------------------
            if (map.FromEnum != null && map.ToEnum != null)
            {
                for (int i = 0; i < map.FromEnum.Count - 1; i++)
                {
                    var tempSqlNode = new SqlNode
                    {
                        Id = map.Id.ToString(),
                        Schema = map.Schema,
                        Table = entityName,
                        Column = map.ToEnum.ElementAt(i).Key,
                        RelationshipKey = $"{entityName}~{map.ToEnum.ElementAt(i).Key}",
                        FromEnumeration = map.FromEnum,
                        ToEnumeration = map.ToEnum,
                        UpsertKeys = map.UpsertKeys.Select(x => $"{x.Entity}~{x.Key}").ToList()
                    };
                    tempSqlNode.LinkKeys.AddRange(linkKeys);
                    
                    SqlNodeRegistry.RegisterNode($"{modelName}~{map.FromEnum.ElementAt(i).Key}", $"{entityName}~{map.ToEnum.ElementAt(i).Key}", tempSqlNode);
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
                    Table = entityName,
                    Column = map.UpsertKeys[i].Key,
                    RelationshipKey = $"{entityName}~{map.ToEnum.ElementAt(i).Key}",
                    FromEnumeration = map.FromEnum,
                    ToEnumeration = map.ToEnum,
                    UpsertKeys = map.UpsertKeys.Select(x => $"{x.Entity}~{x.Key}").ToList()
                };
                tempSqlNode.LinkKeys.AddRange(linkKeys);
                
                SqlNodeRegistry.RegisterNode($"{modelName}~{map.UpsertKeys[i].Key}", $"{entityName}~{map.UpsertKeys[i].Key}", tempSqlNode);
            }
        }
    }
}
