// using System;
// using System.Collections.Generic;
// using CoffeeBeanery.GraphQL.Core.Mapping;
//
// namespace CoffeeBeanery.GraphQL.Core.Mapping
// {
//     public static class MappingHelper
//     {
//         public static void RegisterNodeMap(
//             Dictionary<string, NodeMap> mappings,
//             NodeMap nodeMap,
//             string alias,
//             Type modelType,
//             Type? entityType = null)
//         {
//             nodeMap.ModelType = modelType;
//             nodeMap.EntityType = entityType;
//
//             if (string.IsNullOrEmpty(alias))
//                 alias = modelType.Name;
//
//             // Register top-level NodeMap
//             mappings.TryAdd(alias, nodeMap);
//
//             // Register empty NodeMaps for children to avoid missing mappings
//             foreach (var childAlias in nodeMap.Children)
//             {
//                 if (!mappings.ContainsKey(childAlias))
//                 {
//                     mappings.TryAdd(childAlias, new NodeMap { IsModel = true, IsEntity = true });
//                 }
//             }
//         }
//     }
// }