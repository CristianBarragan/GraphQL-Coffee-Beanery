using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public static class SqlNodeResolver
    {
        // public static (Dictionary<string, SqlNode> node,
        //                Dictionary<string, SqlNode> edge)
        //     ResolveFromSelection<M>(ISyntaxNode selection, string wrapperName)
        // {
        //     // these dictionaries must already be filled by SqlNodeBuilder
        //     var sqlStatementNodeNodes = new Dictionary<string, SqlNode>();
        //     var sqlStatementEdgeNodes = new Dictionary<string, SqlNode>();
        //
        //     // extract all models and entities from tree registry
        //     var modelTrees = SqlNodeRegistry.ModelTrees;
        //     var entityTrees = SqlNodeRegistry.EntityTrees;
        //
        //     var rootTree = entityTrees[wrapperName];
        //     var visitedModels = new List<string>();
        //     var visitedEntities = new List<string>();
        //
        //     // populate select nodes
        //     if (selection != null)
        //     {
        //         // query / mutation root is selection itself
        //         GetFields(entityTrees, selection, SqlNodeRegistry.EntityNodeNodes, SqlNodeRegistry.ModelNodeNodes,
        //             sqlStatementNodeNodes, rootTree, rootTree, visitedModels,
        //             modelTrees.Keys.ToList(), wrapperName,
        //             entityTrees.Keys.ToList(), false);
        //     }
        //     
        //     // populate select edges
        //     if (selection != null)
        //     {
        //         // query / mutation root is selection itself
        //         GetFields(entityTrees, selection, SqlNodeRegistry.EntityEdgeNodes, SqlNodeRegistry.ModelEdgeNodes,
        //             sqlStatementEdgeNodes, rootTree, rootTree, visitedModels,
        //             modelTrees.Keys.ToList(), wrapperName,
        //             entityTrees.Keys.ToList(), false);
        //     }
        //
        //     return (sqlStatementNodeNodes, sqlStatementEdgeNodes);
        // }


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

                        if (previousNode.Split(':').Length == 2)
                        {
                            if (sqlNodeTo.FromEnumeration.TryGetValue(
                                    previousNode.Split(':')[1].Sanitize().Replace("_", ""),
                                    out var enumValue))
                            {
                                var toEnum = sqlNodeTo.FromEnumeration
                                    .FirstOrDefault(e =>
                                    e.Value.Matches(enumValue)).Value;
                                sqlNodeTo.Value = toEnum;
                            }
                            else
                            {
                                sqlNodeTo.Value = previousNode.Split(':')[1].Sanitize();
                            }
                        }

                        AddEntity(linkEntityDictionaryTree, sqlStatementNodes, trees,
                            sqlNodeTo);

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
            Dictionary<string, SqlNode> linkEntityDictionaryTree,
            Dictionary<string, SqlNode> sqlStatementNodes,
            Dictionary<string, NodeTree> trees,
            SqlNode? sqlNode)
        {
            foreach (var entity in linkEntityDictionaryTree
                       .Where(v => sqlNode.Column.Matches(v.Key.Split('~')[1])))
            {
                entity.Value.Value = sqlNode.Value;

                if (!sqlStatementNodes.ContainsKey(entity.Key))
                {
                    sqlStatementNodes.Add(entity.Key, entity.Value);
                }
            }
        }


        // ----------------------------------------------------------
        // Query parsing
        // ----------------------------------------------------------
        public static void GetFields(
            Dictionary<string, NodeTree> trees,
            ISyntaxNode node,
            Dictionary<string, SqlNode> linkEntityDictionaryTree,
            Dictionary<string, SqlNode> linkModelDictionaryTree,
            Dictionary<string, SqlNode> sqlStatementNodes,
            NodeTree currentTree,
            NodeTree parentTree,
            List<string> visitedModels,
            List<string> models,
            string rootEntity,
            List<string> entities,
            bool isEdge)
        {
            if (node != null && node.GetNodes()?.Count() == 0)
            {
                var currentModel = visitedModels.FirstOrDefault();

                if (linkModelDictionaryTree.TryGetValue($"{currentTree.Name}~{node.ToString()}",
                        out var sqlNodeFrom) ||
                    linkModelDictionaryTree.TryGetValue($"{currentModel}~{node.ToString()}",
                        out sqlNodeFrom))
                {
                    if (linkEntityDictionaryTree.TryGetValue(sqlNodeFrom.RelationshipKey,
                            out var sqlNodeTo))
                    {
                        AddField(linkEntityDictionaryTree, sqlStatementNodes, entities,
                            sqlNodeTo, isEdge);

                        if (!visitedModels.Contains(currentTree.Name))
                        {
                            visitedModels.Add(currentTree.Name);
                        }
                    }

                    AddField(linkEntityDictionaryTree, sqlStatementNodes, entities,
                            sqlNodeFrom, isEdge);
                }
            }

            foreach (var childNode in node.GetNodes())
            {
                if (models.Any(e => e.Matches(childNode.ToString().Split('{')[0])) ||
                    childNode.ToString().Matches("nodes") ||
                    childNode.ToString().Matches("node"))
                {
                    if (childNode.ToString().Matches("nodes") ||
                        childNode.ToString().Matches("node"))
                    {
                        currentTree = trees[rootEntity];
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

                GetFields(trees, childNode, linkEntityDictionaryTree, linkModelDictionaryTree,
                    sqlStatementNodes,
                    currentTree,
                    parentTree, visitedModels, models, rootEntity, entities, isEdge);
            }
        }

        private static void AddField(
            Dictionary<string, SqlNode> linkEntityDictionaryTree,
            Dictionary<string, SqlNode> sqlStatementNodes,
            List<string> entities,
            SqlNode? sqlNode,
            bool isEdge)
        {
            foreach (var entity in linkEntityDictionaryTree
                         .Where(v => (sqlNode.Column.Matches(v.Value.Column) ||
                                sqlNode.UpsertKeys.Any(y => v.Key.Split('~')[1].Matches(y.Split("~")[1])))))
            {
                entity.Value.Value = sqlNode.Value;
                entity.Value.SqlNodeType = SqlNodeType.Mutation;

                if (sqlStatementNodes.ContainsKey(entity.Value.RelationshipKey) &&
                    entities.Contains(entity.Value.RelationshipKey.Split("~")[0]))
                {
                    sqlStatementNodes[entity.Value.RelationshipKey] = entity.Value;
                }

                if (!sqlStatementNodes.ContainsKey(entity.Value.RelationshipKey) &&
                    entities.Contains(entity.Value.RelationshipKey.Split("~")[0]))
                {
                    sqlStatementNodes.Add(entity.Value.RelationshipKey, entity.Value);
                }
            }
        }
    }
}
