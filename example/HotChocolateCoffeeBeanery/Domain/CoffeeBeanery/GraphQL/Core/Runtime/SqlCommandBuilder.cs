// using CoffeeBeanery.GraphQL.Core.GraphQL;
// using CoffeeBeanery.GraphQL.Core.Sql;
// using CoffeeBeanery.GraphQL.Helper;
// using HotChocolate.Language;
//
// namespace CoffeeBeanery.GraphQL.Core.Runtime;
//
// public static class SqlCommandBuilder
// {
//    public static void GetMutations(
//         Dictionary<string, NodeTree> trees,
//         ISyntaxNode node,
//         Dictionary<string, SqlNode> linkModelDictionaryTree,
//         Dictionary<string, SqlNode> linkEntityDictionaryTree,
//         Dictionary<string, SqlNode> sqlStatementNodes,
//         NodeTree currentTree,
//         string previousNode,
//         List<string> models)
//     {
//         if (node == null)
//             return;
//
//         var nodeName = node.ToString();
//
//         var colonIndex = nodeName.IndexOf(':');
//         if (colonIndex > 0)
//         {
//             var fieldName = nodeName[..colonIndex].Trim();
//             var rawValue  = nodeName[(colonIndex + 1)..].Trim().Sanitize().Replace("_", "");
//
//             var lookupKey = $"{currentTree.Alias}~{currentTree.Name}~{fieldName}";
//
//             if (linkModelDictionaryTree.TryGetValue(lookupKey, out var sqlNodeFrom) &&
//                 linkEntityDictionaryTree.TryGetValue(sqlNodeFrom.RelationshipKey, out var sqlNodeTo))
//             {
//                 var isEnum = false;
//
//                 var enumValue = sqlNodeTo.FromEnumeration
//                     .FirstOrDefault(a => a.Key.Matches(rawValue));
//
//                 if (!string.IsNullOrEmpty(enumValue.Key))
//                 {
//                     isEnum = true;
//                     sqlNodeTo.Value = enumValue.Value.ToString();
//                 }
//                 else
//                 {
//                     sqlNodeTo.Value = rawValue;
//                 }
//
//                 AddEntity(linkEntityDictionaryTree, sqlStatementNodes,
//                     trees, currentTree, sqlNodeTo, isEnum);
//
//                 return;
//             }
//         }
//         
//         foreach (var childNode in node.GetNodes())
//         {
//             var childText = childNode.ToString();
//
//             var nameNode = childNode.Kind == SyntaxKind.Name
//                 ? childNode
//                 : childNode.GetNodes().FirstOrDefault(n => n.Kind == SyntaxKind.Name);
//
//             var fieldName = nameNode?.ToString();
//
//             var childTree = currentTree;
//
//             if (!string.IsNullOrEmpty(fieldName))
//             {
//                 var modelMatch = models.FirstOrDefault(m => m.Matches(fieldName));
//                 if (modelMatch != null)
//                 {
//                     var matched = trees.FirstOrDefault(t =>
//                         t.Value.Name.Matches(modelMatch)).Value;
//                     if (matched != null)
//                         childTree = matched;
//                 }
//                 
//                 if (childTree == currentTree)
//                 {
//                     var byPrefix = trees.Values.FirstOrDefault(t =>
//                         !string.IsNullOrEmpty(t.Metadata.Prefix) &&
//                         $"{t.Metadata.Prefix}{t.ModelName}".Matches(fieldName));
//                     if (byPrefix != null)
//                         childTree = byPrefix;
//                 }
//                 
//                 if (childTree == currentTree)
//                 {
//                     var byPrefixOnly = trees.Values.FirstOrDefault(t =>
//                         !string.IsNullOrEmpty(t.Metadata.Prefix) &&
//                         t.Metadata.Prefix.Matches(fieldName));
//                     if (byPrefixOnly != null)
//                         childTree = byPrefixOnly;
//                 }
//                 
//                 if (childTree == currentTree)
//                 {
//                     var byModelName = trees.Values.FirstOrDefault(t =>
//                         fieldName.Matches(t.ModelName) ||
//                         fieldName.ToUpperCamelCase().Matches(t.ModelName));
//                     if (byModelName != null)
//                         childTree = byModelName;
//                 }
//                 
//                 if (childTree == currentTree)
//                 {
//                     var byAlias = trees.Values.FirstOrDefault(t =>
//                         fieldName.Matches(t.Alias) ||
//                         fieldName.ToUpperCamelCase().Matches(t.Alias));
//                     if (byAlias != null)
//                         childTree = byAlias;
//                 }
//             }
//
//             GetMutations(trees, childNode,
//                 linkEntityDictionaryTree, linkModelDictionaryTree,
//                 sqlStatementNodes, childTree, childText, models);
//         }
//     } 
//    
//     private static void AddEntity(
//         Dictionary<string, SqlNode> linkEntityDictionaryTree,
//         Dictionary<string, SqlNode> sqlStatementNodes,
//         Dictionary<string, NodeTree> entityTrees,
//         NodeTree currentTree,
//         SqlNode sqlNode,
//         bool isEnum)
//     {
//         if (sqlNode == null) return;
//
//         foreach (var auxFieldMap in currentTree.NodeMap?.FieldMaps
//                      .Where(f => f.SourceName.Matches(sqlNode.RelationshipKey.Split('~')[2])) ?? [])
//         {
//             var entity = sqlNode.Clone() as SqlNode;
//             if (entity == null) continue;
//
//             if (string.IsNullOrEmpty(entity.Value))
//                 entity.Value = sqlNode.Value;
//
//             entity.Alias = auxFieldMap.DestinationAlias;
//             entity.Table = currentTree.Metadata.IsEntity ? currentTree.Name : auxFieldMap.DestinationEntity;
//             entity.RelationshipKey =
//                 $"{(currentTree.Metadata.IsEntity ? currentTree.Alias : auxFieldMap.DestinationAlias)}~" +
//                 $"{(currentTree.Metadata.IsEntity ? currentTree.Name : auxFieldMap.DestinationEntity)}~" +
//                 $"{sqlNode.RelationshipKey.Split('~')[2]}";
//             entity.FromComplexModel = !currentTree.Metadata.IsEntity;
//
//             if (!sqlStatementNodes.ContainsKey(entity.RelationshipKey))
//                 sqlStatementNodes.Add(entity.RelationshipKey, entity);
//         }
//     }
// }