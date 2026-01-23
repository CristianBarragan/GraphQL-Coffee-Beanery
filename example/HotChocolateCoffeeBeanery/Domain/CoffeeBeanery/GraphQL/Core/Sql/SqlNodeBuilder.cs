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

        public static void BuildModel(string modelName, EntityMap map)
        {
            var entityName = map.UpsertKeys.First().Entity;

            // -------------------------
            // Trees
            // -------------------------
            SqlNodeRegistry.ModelTrees.TryAdd(modelName, new NodeTree
            {
                Name = modelName
            });

            SqlNodeRegistry.EntityTrees.TryAdd(entityName, new NodeTree
            {
                Name = entityName,
                Schema = map.Schema
            });

            // -------------------------
            // Field nodes
            // -------------------------
            foreach (var field in map.FieldMaps)
            {
                var key = $"{modelName}~{field.SourceName}";

                // Register regular field nodes
                SqlNodeRegistry.RegisterNode($"{modelName}~{field.SourceName}", $"{entityName}~{field.DestinationName}", new SqlNode
                {
                    Schema = map.Schema,
                    Table = field.DestinationEntity,
                    Column = field.DestinationName,
                    RelationshipKey = $"{entityName}~{field.DestinationName}", // Relationship key
                    SqlNodeType = SqlNodeType.Node,
                    FromEnumeration = map.FromEnum,
                    ToEnumeration = map.ToEnum
                });
                
                SqlNodeRegistry.RegisterEdge($"{modelName}~{field.SourceName}", $"{entityName}~{field.DestinationName}", new SqlNode
                {
                    Schema = map.Schema,
                    Table = field.DestinationEntity,
                    Column = field.DestinationName,
                    RelationshipKey = $"{entityName}~{field.DestinationName}", // Relationship key
                    SqlNodeType = SqlNodeType.Edge,
                    FromEnumeration = map.FromEnum,
                    ToEnumeration = map.ToEnum
                });
                
                SqlNodeRegistry.RegisterMutation($"{modelName}~{field.SourceName}", $"{entityName}~{field.DestinationName}", new SqlNode
                {
                    Schema = map.Schema,
                    Table = field.DestinationEntity,
                    Column = field.DestinationName,
                    RelationshipKey = $"{entityName}~{field.DestinationName}", // Relationship key
                    SqlNodeType = SqlNodeType.Mutation,
                    FromEnumeration = map.FromEnum,
                    ToEnumeration = map.ToEnum
                });
            }

            // -------------------------
            // Enum Mapping for Enums
            // -------------------------
            if (map.FromEnum != null && map.ToEnum != null)
            {
                for (int i = 0; i < map.FromEnum.Count - 1; i++)
                {
                    SqlNodeRegistry.RegisterNode($"{modelName}~{map.FromEnum.ElementAt(i).Key}", $"{entityName}~{map.ToEnum.ElementAt(i).Key}", new SqlNode
                    {
                        Schema = map.Schema,
                        Table = entityName, // Enums are typically mapped within the same entity context
                        Column = map.ToEnum.ElementAt(i).Key,
                        RelationshipKey = $"{entityName}~{map.ToEnum.ElementAt(i).Key}",
                        SqlNodeType = SqlNodeType.Node,
                        FromEnumeration = map.FromEnum,
                        ToEnumeration = map.ToEnum
                    });
                    
                    SqlNodeRegistry.RegisterEdge($"{modelName}~{map.FromEnum.ElementAt(i).Key}", $"{entityName}~{map.ToEnum.ElementAt(i).Key}", new SqlNode
                    {
                        Schema = map.Schema,
                        Table = entityName, // Enums are typically mapped within the same entity context
                        Column = map.ToEnum.ElementAt(i).Key,
                        RelationshipKey = $"{entityName}~{map.ToEnum.ElementAt(i).Key}",
                        SqlNodeType = SqlNodeType.Edge,
                        FromEnumeration = map.FromEnum,
                        ToEnumeration = map.ToEnum
                    });
                    
                    SqlNodeRegistry.RegisterMutation($"{modelName}~{map.FromEnum.ElementAt(i).Key}", $"{entityName}~{map.ToEnum.ElementAt(i).Key}", new SqlNode
                    {
                        Schema = map.Schema,
                        Table = entityName, // Enums are typically mapped within the same entity context
                        Column = map.ToEnum.ElementAt(i).Key,
                        RelationshipKey = $"{entityName}~{map.ToEnum.ElementAt(i).Key}",
                        SqlNodeType = SqlNodeType.Mutation,
                        FromEnumeration = map.FromEnum,
                        ToEnumeration = map.ToEnum
                    });
                }
            }
            
            // -------------------------
            // Mutations (Upsert keys)
            // -------------------------
            for (int i = 0; i < map.UpsertKeys.Count - 1; i++)
            {
                SqlNodeRegistry.RegisterNode($"{modelName}~{map.UpsertKeys[i].Key}", $"{entityName}~{map.UpsertKeys[i].Key}", new SqlNode
                {
                    Schema = map.Schema,
                    Table = entityName, // Enums are typically mapped within the same entity context
                    Column = $"{entityName}~{map.UpsertKeys[i].Key}",
                    RelationshipKey = $"{entityName}~{map.ToEnum.ElementAt(i).Key}",
                    SqlNodeType = SqlNodeType.Node,
                    FromEnumeration = map.FromEnum,
                    ToEnumeration = map.ToEnum
                });
                
                SqlNodeRegistry.RegisterEdge($"{modelName}~{map.UpsertKeys[i].Key}", $"{entityName}~{map.UpsertKeys[i].Key}", new SqlNode
                {
                    Schema = map.Schema,
                    Table = entityName, // Enums are typically mapped within the same entity context
                    Column = $"{entityName}~{map.UpsertKeys[i].Key}",
                    RelationshipKey = $"{entityName}~{map.ToEnum.ElementAt(i).Key}",
                    SqlNodeType = SqlNodeType.Edge,
                    FromEnumeration = map.FromEnum,
                    ToEnumeration = map.ToEnum
                });
                
                SqlNodeRegistry.RegisterMutation($"{modelName}~{map.UpsertKeys[i].Key}", $"{entityName}~{map.UpsertKeys[i].Key}", new SqlNode
                {
                    Schema = map.Schema,
                    Table = entityName, // Enums are typically mapped within the same entity context
                    Column = $"{entityName}~{map.UpsertKeys[i].Key}",
                    RelationshipKey = $"{entityName}~{map.ToEnum.ElementAt(i).Key}",
                    SqlNodeType = SqlNodeType.Mutation,
                    FromEnumeration = map.FromEnum,
                    ToEnumeration = map.ToEnum
                });
            }
        }
    }
}
