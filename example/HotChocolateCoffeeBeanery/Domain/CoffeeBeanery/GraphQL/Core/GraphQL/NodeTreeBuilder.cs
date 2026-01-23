// using System.Collections.Generic;
// using System.Linq;
// using CoffeeBeanery.GraphQL.Core.Mapping;
// using CoffeeBeanery.GraphQL.Core.Sql;
// using KeyNotFoundException = System.Collections.Generic.KeyNotFoundException;
//
// namespace CoffeeBeanery.GraphQL.Core.GraphQL
// {
//     public static class NodeTreeBuilder
//     {
//         public static NodeTree Build(string rootName, Dictionary<string, NodeTree> nodes)
//         {
//             if (!nodes.ContainsKey(rootName))
//                 throw new KeyNotFoundException("Root node not found.");
//
//             var root = nodes[rootName];
//
//             foreach (var node in nodes.Values)
//             {
//                 if (!string.IsNullOrEmpty(node.ParentName))
//                 {
//                     if (nodes.TryGetValue(node.ParentName, out var parent))
//                     {
//                         parent.Children.Add(node);
//                     }
//                 }
//             }
//
//             return root;
//         }
//
//         public static void BuildModel(string modelName, EntityMap map)
//         {
//             var entityName = map.UpsertKeys.First().Entity;
//
//             // -----------------------------
//             // Model Tree
//             // -----------------------------
//             if (!SqlNodeRegistry.ModelTrees.ContainsKey(modelName))
//             {
//                 SqlNodeRegistry.ModelTrees[modelName] = new NodeTree
//                 {
//                     Name = modelName
//                 };
//             }
//
//             // -----------------------------
//             // Entity Tree
//             // -----------------------------
//             if (!SqlNodeRegistry.EntityTrees.ContainsKey(entityName))
//             {
//                 SqlNodeRegistry.EntityTrees[entityName] = new NodeTree
//                 {
//                     Name = entityName,
//                     Schema = map.Schema
//                 };
//             }
//
//             // -----------------------------
//             // Fields
//             // -----------------------------
//             foreach (var field in map.FieldMaps)
//             {
//                 var modelKey = $"{modelName}~{field.SourceName}";
//                 var entityKey = $"{field.DestinationEntity}~{field.DestinationName}";
//
//                 var sqlNode = new SqlNode
//                 {
//                     Schema = map.Schema,
//                     Table = field.DestinationEntity,
//                     Column = field.DestinationName,
//                     RelationshipKey = entityKey,
//                     SqlNodeType = SqlNodeType.Node,
//                     FromEnumeration = map.FromEnum,
//                     ToEnumeration = map.ToEnum
//                 };
//
//                 
//                 
//                 SqlNodeRegistry.RegisterNode(modelKey, sqlNode);
//             }
//             
//             SqlNodeBuilder.BuildModel(model);
//             
//             
//
//             // -----------------------------
//             // Upserts / Mutations
//             // -----------------------------
//             foreach (var upsert in map.UpsertKeys)
//             {
//                 var entityKey = $"{upsert.Entity}~{upsert.Key}";
//
//                 SqlNodeRegistry.RegisterMutation(entityKey, new SqlNode
//                 {
//                     Schema = map.Schema,
//                     Table = upsert.Entity,
//                     Column = upsert.Key,
//                     RelationshipKey = entityKey,
//                     SqlNodeType = SqlNodeType.Mutation
//                 });
//             }
//         }
//     }
// }