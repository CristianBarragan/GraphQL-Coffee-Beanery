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
                if (linkModelDictionaryTree.TryGetValue($"{currentTree.Alias}~{currentTree.Name}~{node.ToString()}",
                        out var sqlNodeFrom))
                {
                    if (linkEntityDictionaryTree.TryGetValue(sqlNodeFrom.RelationshipKey,
                            out var sqlNodeTo))
                    {
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
                            a.Key.Matches($"{currentTree.Alias}~{currentTree.Name}~{node.ToString()}")).Key;

                        AddEntity(sqlStatementNodes, sqlNodeTo, $"{sqlNodeTo.Table}~{key.Split('~')[2]}", value);
                    }
                }
                
                if (!visitedModels.Contains(currentTree.Name))
                {
                    visitedModels.Add(currentTree.Name);
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
    public static void GetFields(Dictionary<string, NodeTree> trees, Dictionary<string, NodeTree> entityTrees, ISyntaxNode node, 
        Dictionary<string,SqlNode> linkEntityDictionaryTree, Dictionary<string,SqlNode> linkModelDictionaryTree,
        Dictionary<string, SqlNode> sqlStatementNodes, NodeTree currentTree,
        NodeTree parentTree, List<string> visitedModels, List<string> visitedEntities, List<string> models, List<string> entities, bool isEdge)
    {
        if (node != null && node.GetNodes()?.Count() == 0)
        {
            var currentModel = visitedModels.LastOrDefault();
            
            var modelTree = trees.FirstOrDefault(a => a.Value.Mapping.Any(b => b.DestinationEntity.Matches(currentTree.Name) &&
                                                                     b.DestinationName.Matches(node.ToString())));
            
            if (
                linkModelDictionaryTree.TryGetValue($"{currentTree.Alias}~{currentTree.Name}~{node.ToString()}", out var sqlNodeFrom)
                ||
                linkModelDictionaryTree.TryGetValue($"{currentTree.Alias}~{modelTree.Value?.Alias}~{node.ToString()}", out sqlNodeFrom))
            {
                if (linkEntityDictionaryTree.TryGetValue(sqlNodeFrom.RelationshipKey, out var sqlNodeTo))
                {
                    AddField(linkEntityDictionaryTree, sqlStatementNodes, currentTree,
                        sqlNodeTo, isEdge);
                    
                    if (!visitedModels.Contains(currentTree.Alias))
                    {
                        visitedModels.Add(currentTree.Alias);
                    }
                }

                visitedEntities.Add(sqlNodeFrom.Table);
                
                AddField(linkEntityDictionaryTree, sqlStatementNodes, currentTree,
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
                    currentTree = trees.OrderBy(a => a.Value.Id).First().Value;
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
            
            GetFields(trees, entityTrees, childNode, linkEntityDictionaryTree, linkModelDictionaryTree, sqlStatementNodes, currentTree, 
                parentTree, visitedModels, visitedEntities, models, entities, isEdge);
        }
    }
    
    private static void AddField(Dictionary<string,SqlNode> linkEntityDictionaryTree,
        Dictionary<string,SqlNode> sqlStatementNodes, NodeTree currentTree, SqlNode? sqlNode, bool isEdge)
    {
        // foreach (var entity in linkEntityDictionaryTree
        //              .Where(v => sqlNode.Column.Matches(v.Value.Column)))
        // {
            var cloned = sqlNode.Clone() as SqlNode;
            cloned.SqlNodeTypes.Clear();
            cloned.SqlNodeTypes.Add((isEdge ? SqlNodeType.Edge : SqlNodeType.Node));
            
            if (sqlStatementNodes.ContainsKey(cloned.RelationshipKey))
            {
                sqlStatementNodes[cloned.RelationshipKey] = cloned;
            }
            
            if (!sqlStatementNodes.ContainsKey(cloned.RelationshipKey))
            {
                sqlStatementNodes.Add(cloned.RelationshipKey, cloned);
            }
        // }
    } 
}
}
