using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public static class SqlNodeResolver
    {
        // ----------------------------------------------------------
        // Mutation parsing
        // ----------------------------------------------------------
        public static void GetMutations(
            Dictionary<string, NodeTree> trees,
            ISyntaxNode node,
            Dictionary<string, SqlNode> linkEntityDictionaryTree,
            Dictionary<string, SqlNode> linkModelDictionaryTree,
            Dictionary<string, SqlNode> sqlStatementNodes,
            NodeTree currentTree,
            string previousNode,
            NodeTree parentTree,
            List<string> models,
            List<string> visitedModels)
        {
            if (node != null && node.GetNodes()?.Count() == 0)
            {
                if (linkModelDictionaryTree.TryGetValue($"{currentTree.Name}~{previousNode.Split(':')[0]}",
                        out var sqlNodeFrom))
                {
                    if (linkEntityDictionaryTree.TryGetValue(sqlNodeFrom.RelationshipKey,
                            out var sqlNodeTo))
                    {
                        sqlNodeTo.SqlNodeType = SqlNodeType.Mutation;
                        
                        var value = string.Empty;

                        if (previousNode.Split(':').Length == 2)
                        {
                            if (sqlNodeTo.FromEnumeration.TryGetValue(
                                    previousNode.Split(':')[1].Sanitize().Replace("_", ""),
                                    out var enumValue))
                            {
                                var toEnum = sqlNodeTo.FromEnumeration
                                    .FirstOrDefault(e =>
                                    e.Value.Matches(enumValue)).Value;
                                value = toEnum;
                            }
                            else
                            {
                                value = previousNode.Split(':')[1].Sanitize();
                            }
                        }

                        var key = linkModelDictionaryTree.First(a =>
                            a.Key.Matches($"{currentTree.Name}~{previousNode.Split(':')[0]}")).Key;

                        AddEntity(sqlStatementNodes, sqlNodeTo, $"{sqlNodeTo.Table}~{key.Split('~')[1]}", value);

                        if (!visitedModels.Contains(currentTree.Name))
                        {
                            visitedModels.Add(currentTree.Name);
                        }
                    }
                }

                return;
            }

            if (node == null)
                return;

            foreach (var childNode in node.GetNodes())
            {
                if (models.Any(e => e.Matches(childNode.ToString().Split('{')[0])) ||
                    node.ToString().Matches("nodes") ||
                    node.ToString().Matches("node"))
                {
                    if (node.ToString().Matches("nodes") ||
                        node.ToString().Matches("node"))
                    {
                        currentTree = trees[models.Last()];
                    }
                    else
                    {
                        currentTree = trees[childNode.ToString().Split('{')[0]];
                    }

                    if (string.IsNullOrWhiteSpace(currentTree.ParentName))
                    {
                        parentTree = currentTree;
                    }
                    else
                    {
                        parentTree = trees[currentTree.ParentName];
                    }
                }

                GetMutations(trees, childNode, linkEntityDictionaryTree, linkModelDictionaryTree,
                    sqlStatementNodes, currentTree, node.ToString(),
                    parentTree, models, visitedModels);
            }
        }

        private static void AddEntity(
            Dictionary<string, SqlNode> sqlStatementNodes,
            SqlNode SqlNodeTo, string key,
            string value)
        {
            if (sqlStatementNodes.ContainsKey(key))
            {
                return;
            }

            var keyValue = new KeyValuePair<string, SqlNode>(key, SqlNodeTo.Clone() as SqlNode);
            keyValue.Value.Value = value;
            
            sqlStatementNodes.Add(keyValue.Key, keyValue.Value);
        }


        /// <summary>
    /// Method for getting upsert field information used for the SQL Statement
    /// </summary>
    /// <param name="trees"></param>
    /// <param name="node"></param>
    /// <param name="linkEntityDictionaryTree"></param>
    /// <param name="linkModelDictionaryTree"></param>
    /// <param name="sqlStatementNodes"></param>
    /// <param name="currentTree"></param>
    /// <param name="parentTree"></param>
    /// <param name="visitedModels"></param>
    /// <param name="models"></param>
    /// <param name="entities"></param>
    /// <param name="isEdge"></param>
    public static void GetFields(Dictionary<string, NodeTree> trees, ISyntaxNode node, 
        Dictionary<string,SqlNode> linkEntityDictionaryTree, Dictionary<string,SqlNode> linkModelDictionaryTree,
        Dictionary<string, SqlNode> sqlStatementNodes, NodeTree currentTree,
        NodeTree parentTree, List<string> visitedModels, List<string> visitedEntities, List<string> models, List<string> entities, bool isEdge)
    {
        if (node != null && node.GetNodes()?.Count() == 0)
        {
            var currentModel = visitedModels.LastOrDefault();
            
            if (linkModelDictionaryTree.TryGetValue($"{currentTree.Name}~{node.ToString()}", out var sqlNodeFrom) ||
                linkModelDictionaryTree.TryGetValue($"{currentModel}~{node.ToString()}", out sqlNodeFrom))
            {
                if (linkEntityDictionaryTree.TryGetValue(sqlNodeFrom.RelationshipKey, out var sqlNodeTo))
                {
                    AddField(linkEntityDictionaryTree, sqlStatementNodes, models, entities,
                        sqlNodeTo, isEdge);
                    
                    if (!visitedModels.Contains(currentTree.Name))
                    {
                        visitedModels.Add(currentTree.Name);
                    }
                }
                visitedEntities.Add(sqlNodeFrom.Table);
                AddField(linkEntityDictionaryTree, sqlStatementNodes, models, entities,
                    sqlNodeFrom, isEdge);
            }
            
            return;
        }

        if (node == null)
        {
            return;
        }
        
        foreach (var childNode in node.GetNodes())
        {
            if (models.Any(e => e.Matches(childNode.ToString().Split('{')[0])) || node.ToString().Matches("nodes") || 
                node.ToString().Matches("node"))
            {
                if (node.ToString().Matches("nodes") || 
                    node.ToString().Matches("node"))
                {
                    currentTree = trees[models.Last()];
                }
                else
                {
                    currentTree = trees[childNode.ToString().Split('{')[0]];
                }

                if (string.IsNullOrWhiteSpace(currentTree.ParentName))
                {
                    parentTree = currentTree;
                }
                else
                {
                    parentTree = trees[currentTree.ParentName];
                }
            }
            
            GetFields(trees, childNode, linkEntityDictionaryTree, linkModelDictionaryTree, sqlStatementNodes, currentTree, 
                parentTree, visitedModels, visitedEntities, models, entities, isEdge);
        }
    }
    
    private static void AddField(Dictionary<string,SqlNode> linkEntityDictionaryTree,
        Dictionary<string,SqlNode> sqlStatementNodes, List<string> models, List<string> entities, SqlNode? sqlNode, bool isEdge)
    {
        foreach (var entity in linkEntityDictionaryTree
                     .Where(v => sqlNode.Column.Matches(v.Value.Column)))
        {
            entity.Value.SqlNodeType = isEdge ? SqlNodeType.Edge : SqlNodeType.Node;
            if (sqlStatementNodes.ContainsKey(entity.Key) &&
                entities.Contains(entity.Key) && isEdge)
            {
                sqlStatementNodes[entity.Key] = entity.Value;
            }
                
            if (!sqlStatementNodes.ContainsKey(entity.Key) &&
                entities.Contains(entity.Key) && !isEdge)
            {
                sqlStatementNodes.Add(entity.Key, entity.Value);
            }
        }
    } 

        // private static void AddField(
        //     Dictionary<string, SqlNode> linkEntityDictionaryTree,
        //     Dictionary<string, SqlNode> sqlStatementNodes,
        //     List<string> entities,
        //     SqlNode? sqlNode,
        //     bool isEdge)
        // {
        //     foreach (var entity in linkEntityDictionaryTree
        //                  .Where(v => (sqlNode.Column.Matches(v.Value.Column) ||
        //                         sqlNode.UpsertKeys.Any(y => v.Key.Split('~')[1].Matches(y.Split("~")[1])))))
        //     {
        //         entity.Value.Value = sqlNode.Value;
        //         entity.Value.SqlNodeType = SqlNodeType.Mutation;
        //
        //         if (sqlStatementNodes.ContainsKey(entity.Value.RelationshipKey) &&
        //             entities.Contains(entity.Value.RelationshipKey.Split("~")[0]))
        //         {
        //             sqlStatementNodes[entity.Value.RelationshipKey] = entity.Value;
        //         }
        //
        //         if (!sqlStatementNodes.ContainsKey(entity.Value.RelationshipKey) &&
        //             entities.Contains(entity.Value.RelationshipKey.Split("~")[0]))
        //         {
        //             sqlStatementNodes.Add(entity.Value.RelationshipKey, entity.Value);
        //         }
        //     }
        // }
    }
}
