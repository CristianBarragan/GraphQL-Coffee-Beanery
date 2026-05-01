using System.Text;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using Dapper;
using FASTER.core;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public class SqlSelectBuilder
{
    
    /// <summary>
    /// Method to handle three Selection using recursion to visit each argument and nodes
    /// </summary>
    /// <param name="graphQlSelection"></param>
    /// <param name="entityTreeMap"></param>
    /// <param name="modelTreeMap"></param>
    /// <param name="rootEntityName"></param>
    /// <param name="wrapperEntityName"></param>
    /// <param name="cache"></param>
    /// <param name="cacheKey"></param>
    /// <param name="permissions"></param>
    /// <typeparam name="D"></typeparam>
    /// <typeparam name="S"></typeparam>
    /// <returns></returns>
    public static SqlStructure HandleGraphQL(ISelection graphQlSelection,
        Dictionary<string, SqlNode> entityDictionary,
        Dictionary<string, SqlNode> modelDictionary,
        Dictionary<string, NodeTree> entityTrees,
        Dictionary<string, NodeTree> modelTrees,
        List<string> entityNames,
        List<string> modelNames, string rootEntityName, string wrapperEntityName,
        IFasterKV<string, string> cache, string cacheKey, string modelName,
        Dictionary<string, List<string>> permissions = null)
    {
        var sqlWhereStatement = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var sqlOrderStatement = string.Empty;
        var parameters = new DynamicParameters();
        var whereFields = new List<string>();
        var pagination = new Pagination();
        var hasPagination = false;
        var hasSorting = false;
        var isCached = false;
        var sqlSelectStatement = string.Empty;
        var sqlUpsertStatement = string.Empty;
        var models = modelNames;
        var transformedToParent = false;
        var tranformedModel = rootEntityName;

        while (!modelNames.Contains(modelName) || rootEntityName.Matches(wrapperEntityName))
        {
            if (modelTrees[modelName].Parents.Count > 0)
            {
                rootEntityName = modelTrees[modelName].Parents[0].From;
                transformedToParent = true;    
            }
            else
            {
                rootEntityName = entityTrees.OrderBy(a => a.Value.Id).First().Value.Name;
            }
        }

        //Where conditions
        GetFieldsWhere(modelTrees, entityDictionary,
            modelDictionary,
            whereFields, sqlWhereStatement, graphQlSelection.SyntaxNode.Arguments
                .FirstOrDefault(a => a.Name.Value.Matches("where")),
            modelTrees.Last().Value.Name, rootEntityName, wrapperEntityName,
            string.Empty, Entity.ClauseTypes, permissions);

        //Arguments
        foreach (var argument in graphQlSelection.SyntaxNode.Arguments
                     .Where(a => !a.Name.Value.Matches("where")))
        {
            switch (argument.Name.ToString())
            {
                case "first":
                    pagination.First = string.IsNullOrEmpty(argument.Value?.Value?.ToString())
                        ? 0
                        : int.Parse(argument.Value?.Value.ToString());
                    hasPagination = true;
                    break;
                case "last":
                    pagination.Last = string.IsNullOrEmpty(argument.Value?.Value?.ToString())
                        ? 0
                        : int.Parse(argument.Value?.Value.ToString());
                    hasPagination = true;
                    break;
                case "before":
                    pagination.Before = string.IsNullOrEmpty(argument.Value?.Value?.ToString())
                        ? ""
                        : argument.Value?.Value.ToString();
                    hasPagination = true;
                    break;
                case "after":
                    pagination.After = string.IsNullOrEmpty(argument.Value?.Value?.ToString())
                        ? ""
                        : argument.Value?.Value.ToString();
                    hasPagination = true;
                    break;
            }

            if (argument.Name.ToString().Contains("order"))
            {
                foreach (var orderNode in argument.GetNodes())
                {
                    hasSorting = true;
                    sqlOrderStatement = GetFieldsOrdering(modelTrees, orderNode,
                        rootEntityName,
                        wrapperEntityName, rootEntityName, modelDictionary);
                }
            }

            var sqlUpsertStatementNodes = new Dictionary<string, SqlNode>();
            var visitedModels = new List<string>()
            {
                modelName
            };

            if (argument.GetNodes().Last(a => a.Kind == SyntaxKind.ObjectValue).GetNodes().Any(a => a.ToString().StartsWith(modelName.ToLowerCamelCase())))
            {
                var nodeTreeRoot = new NodeTree();
                nodeTreeRoot.Name = string.Empty;
                var mutationNodeToProcess = argument.GetNodes().Last(a => a.Kind == SyntaxKind.ObjectValue).GetNodes()
                    .First(a => a.ToString().StartsWith(modelName.ToLowerCamelCase()));
            
                var generatedQuery = new List<string>();
                var sqlUpsertBuilder = new StringBuilder();
                var sqlSelectUpsertBuilder = new StringBuilder();
            
                if (mutationNodeToProcess.GetNodes().ToList()[1].ToString().StartsWith("["))
                {
                    foreach (var mutationNode in mutationNodeToProcess.GetNodes().ToList()[1].GetNodes())
                    {
                        GetMutations(modelTrees, mutationNode,
                            entityDictionary, modelDictionary,
                            sqlUpsertStatementNodes, modelTrees
                                .First(t =>
                                    t.Key.Matches(rootEntityName)).Value, string.Empty,
                            new NodeTree(), models, entityNames, visitedModels);
                        
                        SqlHelper.GenerateUpsertStatements(entityTrees, entityDictionary, rootEntityName,
                            wrapperEntityName, generatedQuery, sqlUpsertStatementNodes, entityTrees[rootEntityName], entityNames,
                            sqlWhereStatement, new List<string>(),
                            sqlUpsertBuilder, sqlSelectUpsertBuilder);
                    }
                }
                else
                {
                    GetMutations(modelTrees, mutationNodeToProcess,
                        entityDictionary, modelDictionary,
                        sqlUpsertStatementNodes, modelTrees
                            .First(t =>
                                t.Key.Matches(rootEntityName)).Value, string.Empty,
                        new NodeTree(), models, entityNames, visitedModels);
                    
                    SqlHelper.GenerateUpsertStatements(entityTrees, entityDictionary, rootEntityName,
                        wrapperEntityName, generatedQuery, sqlUpsertStatementNodes, entityTrees[rootEntityName], entityNames,
                        sqlWhereStatement, new List<string>(),
                        sqlUpsertBuilder, sqlSelectUpsertBuilder);
                }
                sqlUpsertStatement = sqlUpsertBuilder.ToString() + " ; " + sqlSelectUpsertBuilder.ToString();
            }
        }

        // Query Select
        var level = 1;
        var rootNodeTree = new NodeTree();

        //Generate cache level 1
        var edgeNode = graphQlSelection.SelectionSet?.Selections!
            .FirstOrDefault(s => s.ToString().StartsWith("edges"));
        var node = graphQlSelection.SelectionSet?.Selections!
            .FirstOrDefault(s => s.ToString().StartsWith("nodes"));

        //Read cache
        // using var cacheReadSession = cache.NewSession(new SimpleFunctions<string, string>());
        // cacheReadSession.Read(ref cacheKey, ref sqlSelectStatement);

        var sqlStatementNodes = new Dictionary<string, SqlNode>();
        var visitedFieldModel = new List<string>();

        if (graphQlSelection.SelectionSet?.Selections!
                .FirstOrDefault(s => s.ToString().StartsWith("edges")) != null)
        {
            GetFields(modelTrees, edgeNode.GetNodes().ToList()[1].GetNodes()
                    .ToList()[0],
                entityDictionary, modelDictionary,
                sqlStatementNodes,
                entityTrees.First(t =>
                    t.Key.Matches(rootEntityName)).Value,
                new NodeTree(), visitedFieldModel, models, rootEntityName, entityNames,
                true);
        }
        
        visitedFieldModel.Clear();

        if (graphQlSelection.SelectionSet?.Selections!
                .FirstOrDefault(s => s.ToString().StartsWith("nodes")) != null)
        {
            GetFields(modelTrees, node,
                entityDictionary, modelDictionary,
                sqlStatementNodes,
                modelTrees.First(t =>
                    t.Key.Matches(rootEntityName)).Value,
                new NodeTree(), visitedFieldModel, models, rootEntityName, entityNames,
                false);
        }

        var sqlQueryStatement = new StringBuilder();
        var sqlQueryStructures = new Dictionary<string, SqlQueryStructure>(
            StringComparer.OrdinalIgnoreCase);
        var splitOnDapper = new Dictionary<string, Type>();
        var entityOrder = new List<string>();

        if (string.IsNullOrEmpty(sqlSelectStatement))
        {
            var childrenSqlStatement = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            var entityTypes = entityTrees.Select(a => a.Value.EntityType).ToList();

            GenerateQuery(entityTrees,
                entityTypes,
                entityDictionary,
                sqlQueryStatement, sqlStatementNodes, sqlWhereStatement,
                entityTrees[rootEntityName],
                childrenSqlStatement, entityNames, sqlQueryStructures,
                splitOnDapper, entityOrder, rootEntityName);
            
            //if transformedToParent then will be used the first matching child, TODO: Support multiple child queries for complex entities

            var queryStructure = sqlQueryStructures.FirstOrDefault();

            if (transformedToParent)
            {
                splitOnDapper.Remove(splitOnDapper.First(a => a.Value.Name == rootEntityName).Key);
                
                foreach (var childName in entityTrees[rootEntityName].Children.Select(a => a.To))
                {
                    if (modelDictionary.FirstOrDefault(a => a.Key.Split('~')[0].Matches(tranformedModel)).Value.RelationshipKey.Split('~')[1]
                        .Matches(childName))
                    {
                        queryStructure = sqlQueryStructures.FirstOrDefault(s => s.Key.Matches(childName));
                        if (queryStructure.Value != null)
                        {
                            break;
                        }    
                    }
                    else
                    {
                        splitOnDapper.Remove(splitOnDapper.First(a => a.Value.Name == childName).Key);
                    }
                }
            }

            sqlSelectStatement = queryStructure.Value.Query;

            if (splitOnDapper.Count == 0)
            {
                splitOnDapper.Add(queryStructure.Value.JoinOneKey, entityTypes
                    .First(a => a.Name.Matches(queryStructure.Key)));
            }

            //Update cache
            // if (!isCached)
            // {
            //     cacheReadSession.Upsert(ref cacheKey, ref sqlStament);    
            // }
        }

        var splitOnDapperOrdered = new Dictionary<string, Type>();

        foreach (var key in entityOrder)
        {
            var kv = splitOnDapper.FirstOrDefault(t => t.Value.Name.Matches(key));

            if (kv.Value != null && !splitOnDapperOrdered.ContainsKey(kv.Key))
            {
                splitOnDapperOrdered.Add(kv.Key, kv.Value);
            }
        }

        if (splitOnDapperOrdered.Count == 0)
        {
            var entity = entityTrees[rootEntityName].EntityType;
            splitOnDapperOrdered.Add(entity.Name, entity);
        }

        if (string.IsNullOrEmpty(sqlSelectStatement))
        {
            return default;
        }

        var hasTotalCount = false;

        if (hasPagination || hasSorting)
        {
            rootNodeTree = entityTrees[rootEntityName];
            // Query Where, Sort, and Pagination
            sqlSelectStatement = SqlHelper.HandleQueryClause(rootNodeTree, sqlSelectStatement,
                sqlOrderStatement, pagination, hasTotalCount);
        }

        var sqlStructure = new SqlStructure()
        {
            SqlQuery = sqlSelectStatement,
            Parameters = parameters,
            SqlUpsert = sqlUpsertStatement,
            SplitOnDapper = splitOnDapperOrdered,
            Pagination = pagination,
            HasTotalCount = false
        };

        return sqlStructure;
    }
    
    public static void GetMutations(Dictionary<string, NodeTree> trees, ISyntaxNode node,
        Dictionary<string, SqlNode> linkEntityDictionaryTree,
        Dictionary<string, SqlNode> linkModelDictionaryTree,
        Dictionary<string, SqlNode> sqlStatementNodes, NodeTree currentTree,
        string previousNode, NodeTree parentTree, List<string> models, List<string> entities,
        List<string> visitedModels)
    {
        if (node != null && node.GetNodes()?.Count() == 0)
        {
            if (linkModelDictionaryTree.TryGetValue(
                $"{currentTree.Alias}~{currentTree.Name}~{node.ToString()}",
                out var sqlNodeFrom))
            {
                if (linkEntityDictionaryTree.TryGetValue(sqlNodeFrom.RelationshipKey,
                        out var sqlNodeTo))
                {
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

                    AddEntity(linkEntityDictionaryTree, sqlStatementNodes, models, entities,
                        sqlNodeTo);
                }

                if (previousNode.Split(':').Length == 2)
                {
                    if (sqlNodeFrom.ToEnumeration.TryGetValue(previousNode.Split(':')[1]
                                .Sanitize().Replace("_", ""),
                            out var enumValue))
                    {
                        sqlNodeFrom.Value = enumValue;
                    }
                    else
                    {
                        sqlNodeFrom.Value = previousNode.Split(':')[1].Sanitize();
                    }
                }

                AddEntity(linkEntityDictionaryTree, sqlStatementNodes, models, entities,
                    sqlNodeFrom);
            }

            return;
        }

        if (node == null)
        {
            return;
        }

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

                if (currentTree.Parents.Count == 0)
                {
                    parentTree = currentTree;
                }
                else
                {
                    parentTree = trees[currentTree.Parents[0].To];
                }
            }

            GetMutations(trees, childNode, linkEntityDictionaryTree, linkModelDictionaryTree,
                sqlStatementNodes, currentTree, node.ToString(),
                parentTree, models, entities, visitedModels);
        }
    }
    
    private static void AddEntity(Dictionary<string, SqlNode> linkEntityDictionaryTree,
        Dictionary<string, SqlNode> sqlStatementNodes, List<string> models, List<string> entities,
        SqlNode? sqlNode)
    {
        foreach (var entity in linkEntityDictionaryTree
                     .Where(v => sqlNode.Column.Matches(v.Value.Column)))
        {
            entity.Value.Value = sqlNode.Value;
            entity.Value.SqlNodeTypes.Add(SqlNodeType.Mutation);
            if (sqlStatementNodes.ContainsKey(entity.Value.RelationshipKey) &&
                entities.Contains(entity.Value.RelationshipKey.Split("~")[1]) &&
                !models.Contains(entity.Value.RelationshipKey.Split("~")[2]))
            {
                sqlStatementNodes[entity.Value.RelationshipKey] = entity.Value;
            }

            if (!sqlStatementNodes.ContainsKey(entity.Value.RelationshipKey) &&
                entities.Contains(entity.Value.RelationshipKey.Split("~")[1]) &&
                !models.Contains(entity.Value.RelationshipKey.Split("~")[2]))
            {
                sqlStatementNodes.Add(entity.Value.RelationshipKey, entity.Value);
            }
        }
    }
    
    public static void GetFields(Dictionary<string, NodeTree> trees, ISyntaxNode node,
        Dictionary<string, SqlNode> linkEntityDictionaryTree,
        Dictionary<string, SqlNode> linkModelDictionaryTree,
        Dictionary<string, SqlNode> sqlStatementNodes, NodeTree currentTree,
        NodeTree parentTree, List<string> visitedModels, List<string> models,
        string rootEntity, List<string> entities, bool isEdge)
    {
        if (node != null && node.GetNodes()?.Count() == 0)
        {
            if (linkEntityDictionaryTree.TryGetValue($"{currentTree.Alias}~{currentTree.Name}~{node.ToString()}",
                    out var sqlNodeFrom)
               )
            {
                if (linkModelDictionaryTree.TryGetValue(sqlNodeFrom.RelationshipKey,
                        out var sqlNodeTo))
                {
                    
                    AddField(linkEntityDictionaryTree, sqlStatementNodes, entities,
                        sqlNodeTo, isEdge);
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

                if (currentTree.Parents.Count == 0)
                {
                    parentTree = currentTree;
                }
                else
                {
                    parentTree = trees[currentTree.Parents[0].To];
                }
            }

            GetFields(trees, childNode, linkEntityDictionaryTree, linkModelDictionaryTree,
                sqlStatementNodes,
                currentTree,
                parentTree, visitedModels, models, rootEntity, entities, isEdge);
        }
    }
    
    private static void AddField(Dictionary<string, SqlNode> linkEntityDictionaryTree,
        Dictionary<string, SqlNode> sqlStatementNodes, List<string> entities, SqlNode? sqlNode,
        bool isEdge)
    {
        foreach (var entity in linkEntityDictionaryTree
                     .Where(v => (sqlNode.Column.Matches(v.Value.Column) || 
                                  sqlNode.UpsertKeys.Any(y => v.Key.Split('~')[1].Matches(y.Split("~")[1])) ||
                                  sqlNode.EntityRelatedChildren.Any(y => v.Key.Split('~')[1].Matches(y.From.Split("~")[1]))) ||
                                 sqlNode.EntityRelatedChildren.Any(y => v.Key.Split('~')[1].Matches(y.To.Split("~")[1]))))
        {
            var entityCloned = (SqlNode)entity.Value.Clone();
            entityCloned.Value = sqlNode.Value;
            entityCloned.SqlNodeTypes.Clear();
            entity.Value.SqlNodeTypes.Clear();
            entityCloned.SqlNodeTypes.Add(isEdge ? SqlNodeType.Edge : SqlNodeType.Node);
            entity.Value.SqlNodeTypes.Add(isEdge ? SqlNodeType.Edge : SqlNodeType.Node);
            if (sqlStatementNodes.ContainsKey(entity.Value.RelationshipKey) &&
                entities.Contains(entity.Value.RelationshipKey.Split("~")[0]))
            {
                sqlStatementNodes[entity.Value.RelationshipKey] = entityCloned;
            }

            if (!sqlStatementNodes.ContainsKey(entity.Value.RelationshipKey) &&
                entities.Contains(entity.Value.RelationshipKey.Split("~")[0]))
            {
                sqlStatementNodes.Add(entity.Value.RelationshipKey, entityCloned);
            }
        }
    }
    
    private static bool
        GenerateQuery(Dictionary<string, NodeTree> entityTrees,
            List<Type> entityTypes,
            Dictionary<string, SqlNode> linkEntityDictionaryTreeNode,
            StringBuilder sqlQueryStatement, Dictionary<string, SqlNode> sqlStatementNodes,
            Dictionary<string, string> sqlWhereStatement,
            NodeTree currentTree, Dictionary<string, string> childrenSqlStatement,
            List<string> entityNames,
            Dictionary<string, SqlQueryStructure> sqlQueryStructures,
            Dictionary<string, Type> splitOnDapper, List<string> entityOrder, string rootNodeTree)
    {
        var hasChildren = false;
        var currentEntityStructure = GenerateEntityQuery(entityTrees,
            linkEntityDictionaryTreeNode,
            sqlStatementNodes, currentTree, entityNames, sqlQueryStatement,
            sqlQueryStructures, sqlWhereStatement, childrenSqlStatement, rootNodeTree, hasChildren);

        
        if (!sqlQueryStructures.Any(a => a.Key
                .Matches(currentTree.Alias)))
        {
            sqlQueryStructures.Add(currentTree.Alias, currentEntityStructure);
        }
        
        var queryBuilder = $"SELECT % FROM \"{currentTree.Schema}\".\"{currentTree.Name}\" {currentTree.Alias} ";
        currentEntityStructure.SelectColumns.AddRange(currentEntityStructure.Columns);
        currentEntityStructure.SelectColumns = currentEntityStructure.SelectColumns.Distinct().ToList();

        currentTree = entityTrees[currentTree.Alias];

        if (!entityOrder.Contains(currentTree.Alias))
        {
            entityOrder.Add(currentTree.Alias);    
        }
        
        foreach (var child in currentTree.Children)
        {
            var childTree = entityTrees[child.To];
            
            // if (childTree.Children.Count == 0 && (!sqlQueryStructures.Any(a => a.Key.Matches(childTree.Alias)) ||  
            //         (sqlQueryStructures.Any(a => a.Key.Matches(childTree.Alias)) && 
            //         sqlQueryStructures[childTree.Alias].Columns.Count == 1)))
            // {
            //     continue;
            // }
            
            if (currentTree.Children.Any(k => k.To.Matches(child.To)))
            {
                GenerateQuery(entityTrees, entityTypes, linkEntityDictionaryTreeNode,
                    sqlQueryStatement, sqlStatementNodes, sqlWhereStatement,
                    childTree, childrenSqlStatement, entityNames, sqlQueryStructures,
                    splitOnDapper, entityOrder, rootNodeTree);

                var childStructure = sqlQueryStructures[childTree.Alias];
                queryBuilder += childStructure.SqlNodeType == SqlNodeType.Edge ? " JOIN " : " LEFT JOIN ";
                queryBuilder +=
                    $" ( {childStructure.Query} ) {childTree.Alias} ON {(string.IsNullOrEmpty(
                        childStructure.JoinOneKey) ? $"{currentTree.Alias}.\"Id\"" : $"{childTree.Alias}.\"{childStructure.JoinOneKey}\"")} = {
                        childTree.Alias}.\"{"Id".ToSnakeCase(childTree.Id)}\"";

                currentEntityStructure.SelectColumns.AddRange(
                    childStructure.ParentColumns.Select(s => s.Replace("~", childTree.Alias)));
                currentEntityStructure.ParentColumns.AddRange(childStructure.ParentColumns);

                currentEntityStructure.SelectColumns = currentEntityStructure.SelectColumns.Distinct().ToList();
                currentEntityStructure.ParentColumns = currentEntityStructure.ParentColumns.Distinct().ToList();

                if (!splitOnDapper.ContainsKey("Id".ToSnakeCase(childTree.Id)))
                {
                    splitOnDapper.Add("Id".ToSnakeCase(childTree.Id),
                        entityTypes.FirstOrDefault(e => e.Name.Matches(childTree.Name)));
                }
            }
        }

        var select = string.Join(",", currentEntityStructure.SelectColumns.DistinctBy(a => a.Split(" AS ")[1]));

        queryBuilder = queryBuilder.Replace("%", select);
        queryBuilder += " " + currentEntityStructure.WhereClause;

        currentEntityStructure.Query = queryBuilder;
        var currentNode = linkEntityDictionaryTreeNode.FirstOrDefault(a =>
            a.Key.Contains(currentTree.Alias));
        currentEntityStructure.Id = currentTree.Id;
        currentEntityStructure.SqlNode = currentNode.Value;

        if (sqlQueryStructures.TryGetValue(currentTree.Alias, out var sqlQueryStructure))
        {
            sqlQueryStructures[currentTree.Alias] = currentEntityStructure;
        }
        else
        {
            sqlQueryStructures.Add(currentTree.Alias, currentEntityStructure);
        }

        if (!splitOnDapper.ContainsKey("Id".ToSnakeCase(currentTree.Id)))
        {
            splitOnDapper.Add("Id".ToSnakeCase(currentTree.Id),
                entityTypes.FirstOrDefault(e => e.Name.Matches(currentTree.Name)));
        }

        return true;
    }
    
    private static SqlQueryStructure GenerateEntityQuery(Dictionary<string, NodeTree> entityTrees,
        Dictionary<string, SqlNode> linkEntityDictionaryTreeNode,
        Dictionary<string, SqlNode> sqlStatementNodes, NodeTree currentTree, List<string> entityNames,
        StringBuilder sqlQueryStatement, Dictionary<string, SqlQueryStructure> sqlQueryStructures,
        Dictionary<string, string> sqlWhereStatement, Dictionary<string, string> childrenSqlStatement,
        string rootEntity, bool hasChildren)
    {
        var childrenJoinColumns = new Dictionary<string, string>();
        var currentColumns = new List<KeyValuePair<string, SqlNode>>();
        
        var columnToAdd = linkEntityDictionaryTreeNode
            .FirstOrDefault(k => k.Key
                .Matches($"{currentTree.Alias}~{currentTree.Name}~Id"));
        columnToAdd.Value.SqlNodeTypes = new List<SqlNodeType>()
        {
            SqlNodeType.Node
        };
        
        if (columnToAdd.Value != null)
        {
            currentColumns.Add(columnToAdd);
        }

        currentColumns.AddRange(sqlStatementNodes
            .Where(k => k.Key.Split('~')[0].Matches(currentTree.Alias) && 
                        entityNames.Contains(k.Value.RelationshipKey.Split('~')[1]) &&
                        ! k.Value.LinkKeys.Any(b => b.From.Matches(k.Key)) &&
                        ! k.Value.LinkKeys.Any(b => entityTrees.Keys.Any(a => a.Matches(k.Key.Split('~')[2])))).ToList());
        
        if (currentColumns.Count == 0)
        {
            return new SqlQueryStructure();
        }

        currentColumns = currentColumns.Distinct().ToList();

        var queryBuilder = string.Empty;
        var queryColumns = new List<string>();
        var parentQueryColumns = new List<string>();

        foreach (var tableColumn in currentColumns)
        {
            var tableFieldParts = tableColumn.Key.Split('~');
            var fieldName = tableFieldParts[2];

            if (!queryColumns.Contains($"\"{fieldName
                .ToSnakeCase(currentTree.Id)}\""))
            {
                queryColumns.Add(
                    $"{currentTree.Alias}.\"{fieldName}\" AS \"{fieldName
                        .ToSnakeCase(currentTree.Id)}\"");
            }

            if (!parentQueryColumns.Contains($"\"{fieldName.ToSnakeCase(currentTree.Id)}\""))
            {
                parentQueryColumns.Add(
                    $"~.\"{fieldName.ToSnakeCase(currentTree.Id)}\" AS \"{fieldName.ToSnakeCase(currentTree.Id)}\"");
            }
        }

        foreach (var childQuery in sqlQueryStructures
                     .Where(c =>
                     currentTree.Children
                         .Any(b => b.To.Matches(c.Key))))
        {
            queryBuilder += $" {(childQuery.Value.SqlNodeType == SqlNodeType.Edge ?
                " JOIN ( " : " LEFT JOIN  ( ")} {childQuery.Value.Query}";
            
            var joinChildKey = $"\"{"Id"
                .ToSnakeCase(childQuery.Value.Id)}\"";

            if (currentColumns.Count > 0)
            {
                var linkKeys = currentColumns[0].Value.LinkKeys
                    .Where(k => k.To.Matches(childQuery.Key)).ToList();

                if (linkKeys.Count == 0)
                {
                    for (var i = 0; i < linkKeys.Count; i++)
                    {
                        if (i == 0)
                        {
                            queryBuilder +=
                                $" ) {childQuery.Key} ON {currentTree.Name}.\"Id\" = {childQuery.Key}.{joinChildKey}";
                        }
                        else
                        {
                            queryBuilder +=
                                $" AND {childQuery.Key} ON {currentTree.Name}.\"Id\" = {childQuery.Key}.{joinChildKey}";
                        }
                    }
                }
            }
        }

        var entitySqlWhereStatement = string.Empty;

        if (currentColumns.Count <= 2 && childrenSqlStatement.Count > 0)
        {
            var newRootNodeTree = entityTrees[childrenSqlStatement.First().Key];
            sqlWhereStatement.TryGetValue(newRootNodeTree.Name, out var
                currentSqlWhereStatementNewRoot);
            var oldWhereStatement = currentSqlWhereStatementNewRoot;

            if (!string.IsNullOrEmpty(oldWhereStatement))
            {
                oldWhereStatement = oldWhereStatement.Replace("~", newRootNodeTree.Name);

                foreach (var field in oldWhereStatement.Split("\""))
                {
                    oldWhereStatement =
                        oldWhereStatement.Replace(field, $"{field}");
                }

                oldWhereStatement = $" WHERE {oldWhereStatement} ";
            }
            else
            {
                oldWhereStatement = string.Empty;
            }

            currentSqlWhereStatementNewRoot = string.IsNullOrEmpty(currentSqlWhereStatementNewRoot)
                ? string.Empty
                : currentSqlWhereStatementNewRoot;

            if (childrenSqlStatement.Count > 0 && childrenSqlStatement.Count > 0 &&
                !string.IsNullOrEmpty(currentSqlWhereStatementNewRoot))
            {
                var cutoff = childrenSqlStatement.First().Value.IndexOf('(') + 1;
                var sqlStatement =
                    $"{childrenSqlStatement.First().Value.Substring(cutoff,
                        childrenSqlStatement.First()
                        .Value.Length - cutoff)}";
                sqlStatement = sqlStatement.Replace(oldWhereStatement,
                    $" WHERE {currentSqlWhereStatementNewRoot.Replace("~", newRootNodeTree.Name)}");

                sqlQueryStatement.Append(sqlStatement);
            }
        }
        else
        {
            sqlQueryStatement.Append(queryBuilder);
            queryBuilder = "";
            queryBuilder += " SELECT % ";
            queryBuilder += $" FROM \"{currentTree.Schema}\".\"{currentTree.Name}\" {currentTree.Alias}";

            var model = linkEntityDictionaryTreeNode
                .FirstOrDefault(e =>
                e.Key.Split('~')[0].Matches(currentTree.Name));

            var modelValue = string.Empty;
            if (model.Value != null)
            {
                modelValue = currentTree?.Mapping?.FirstOrDefault(m =>
                    m.DestinationEntity.Matches(currentTree.Name))?.SourceModel ?? string.Empty;
            }

            if (string.IsNullOrEmpty(modelValue))
            {
                modelValue = currentTree.Name;
            }

            if (sqlWhereStatement.TryGetValue(modelValue, out var currentSqlWhereStatement))
            {
                currentSqlWhereStatement = currentSqlWhereStatement.Replace("~", currentTree.Alias);
                entitySqlWhereStatement = $" WHERE {currentSqlWhereStatement} ";
            }
            else
            {
                entitySqlWhereStatement = string.Empty;
            }
        }

        var joinOneKey = string.Empty;
        var onJoinKey = string.Empty;
        var currentJoinOneKeys = currentColumns.FirstOrDefault(a => a.Key.Split('~')[0].Matches(currentTree.Alias)).Value
            .EntityRelatedChildren;

        if (currentJoinOneKeys.Count() > 0
            &&
            currentJoinOneKeys[0].From.Split('~')[0].Matches(currentTree.Name))
        {
            var oneKey = currentJoinOneKeys.FirstOrDefault();
            
            joinOneKey = $"{currentTree.Alias}.\"{oneKey.ToColumn}\" AS \"{oneKey.To}{oneKey
                .ToColumn.ToSnakeCase(currentTree.Id)}\"";
            var joinOneKeyParent = $"~.\"{oneKey.ToColumn.ToSnakeCase(currentTree.Id)}\" AS \"{oneKey
                .To}{oneKey.ToColumn.ToSnakeCase(currentTree.Id)}\"";
            queryColumns.Add(joinOneKey);
            parentQueryColumns.Add(joinOneKeyParent);
            onJoinKey = $"{oneKey.To}{oneKey.ToColumn.ToSnakeCase(currentTree.Id)}";
        }

        queryColumns = queryColumns.Distinct().ToList();
        var select = string.Join(",", queryColumns);
        queryBuilder = queryBuilder.Replace("%", select);

        var sqlStructure = new SqlQueryStructure()
        {
            Id = currentTree.Id,
            SqlNodeType = currentColumns.Count > 1 && currentColumns.Last().Value.SqlNodeTypes.Count > 0 ? currentColumns.Last().Value.SqlNodeTypes[0] : SqlNodeType.Node,
            SqlNode = currentColumns.Count > 0 ? currentColumns.Last().Value :
                new SqlNode(),
            Query = queryBuilder,
            Columns = queryColumns,
            ParentColumns = parentQueryColumns,
            ChildrenJoinColumns = childrenJoinColumns,
            WhereClause = entitySqlWhereStatement,
            JoinOneKey = onJoinKey
        };

        if (currentTree.Alias.Matches(rootEntity))
        {
            var addingMissingUpsertKeys = linkEntityDictionaryTreeNode
                .First(c => c.Key.Split('~')[0].Matches(currentTree.Alias))
                .Value.UpsertKeys.Where(u => !sqlStructure.Columns.Any(a => a
                    .Matches($"{currentTree.Alias}.\"Id\" AS \"{u.Split('~')[1].ToSnakeCase(currentTree.Id)}\"")))
                    .Select(a => $"{currentTree.Alias}.\"{a.Split('~')[1]}\" AS \"{a.Split('~')[1].ToSnakeCase(currentTree.Id)}\"");

            var addingMissingUpsertKeysParent = linkEntityDictionaryTreeNode
                .First(c => c.Key.Split('~')[0].Matches(currentTree.Alias))
                .Value.UpsertKeys.Where(u => !sqlStructure.Columns.Any(a => a
                    .Matches($"{currentTree.Alias}.\"Id\" AS \"{u.Split('~')[1].ToSnakeCase(entityTrees[currentTree.Alias].Id)}\"")))
                .Select(a => $"{currentTree.Alias}.\"{a.Split('~')[1].ToSnakeCase(entityTrees[currentTree.Alias]
                    .Id)}\" AS \"{a.Split('~')[1].ToSnakeCase(entityTrees[currentTree.Alias].Id)}\"");

            if (addingMissingUpsertKeys != null && addingMissingUpsertKeys.Count() > 0)
            {
                sqlStructure.Columns.AddRange(addingMissingUpsertKeys);

                foreach (var key in addingMissingUpsertKeysParent)
                {
                    if (!sqlStructure.ParentColumns.Contains(key))
                    {
                        sqlStructure.ParentColumns.Add(key);
                    }
                }
            }

            sqlStructure.Columns = sqlStructure.Columns.Distinct().ToList();
        }

        return sqlStructure;
    }
    
    public static string GetFieldsOrdering(Dictionary<string, NodeTree> trees,
        ISyntaxNode orderNode, string entity,
        string wrapperEntity, string rootEntity, Dictionary<string, SqlNode> linkModelDictionaryTree)
    {
        var orderString = string.Empty;
        foreach (var oNode in orderNode.GetNodes())
        {
            var currentEntity = entity;
            if (oNode.ToString().Contains("{") && oNode.ToString()[0] != '{' &&
                oNode.ToString().Contains(":"))
            {
                currentEntity = oNode.ToString().Split(":")[0];
            }

            if (!oNode.ToString().Contains("{") && oNode.ToString().Contains(":"))
            {
                var column = oNode.ToString().Split(":");
                if ((column[1].Contains("DESC") || column[1].Contains("ASC")) &&
                    trees.ContainsKey(currentEntity))
                {
                    currentEntity = currentEntity.Matches(wrapperEntity) ? rootEntity :
                        currentEntity;
                    var currentNodeTree = trees[currentEntity];
                    orderString +=
                        SqlGraphQlHelper.HandleSort(currentNodeTree, column[0],
                            column[1], linkModelDictionaryTree);
                }
            }

            orderString +=
                $", {GetFieldsOrdering(trees, oNode, wrapperEntity, rootEntity, currentEntity,
                    linkModelDictionaryTree)}";
        }

        return orderString;
    }

    public static void GetFieldsWhere(Dictionary<string, NodeTree> trees,
        Dictionary<string, SqlNode> linkEntityDictionaryTreeNode,
        Dictionary<string, SqlNode> linkModelDictionaryTreeNode, List<string> whereFields,
        Dictionary<string, string> sqlWhereStatement,
        ISyntaxNode whereNode, string entityName, string rootEntityName, string wrapperEntityName,
        string clauseCondition,
        List<string> clauseType,
        Dictionary<string, List<string>> permission = null)
    {
        if (whereNode == null || string.IsNullOrWhiteSpace(entityName))
        {
            return;
        }

        foreach (var wNode in whereNode.GetNodes())
        {
            if (wrapperEntityName.Matches(entityName))
            {
                entityName = rootEntityName;
            }

            var currentEntity = entityName;

            currentEntity = trees.Keys.FirstOrDefault(e => e.ToString()
                .Matches(wNode.ToString().Split(":")[0]));

            if (string.IsNullOrEmpty(currentEntity) || currentEntity.Matches(rootEntityName))
            {
                currentEntity = entityName;
            }

            if (whereNode.ToString().TrimStart(' ').StartsWith("and:") ||
                whereNode.ToString().TrimStart(' ').StartsWith("or:"))
            {
                clauseCondition = whereNode.ToString().Split("{")[0].Replace(":", "").ToUpper();
            }

            if (wNode.ToString().Contains("{") && wNode.ToString().Contains(":") &&
                wNode.ToString().Split(":").Length == 3)
            {
                var column = wNode.ToString().Split(":")[0];

                if (!column.Contains("{"))
                {
                    if (linkModelDictionaryTreeNode.TryGetValue($"{currentEntity}~{column}",
                            out var currentKeyValueNode))
                    {
                        var fieldValue = currentKeyValueNode.RelationshipKey.Replace('~', '.');
                        currentEntity = $"{currentKeyValueNode.RelationshipKey.Split('~')[0]}";
                        whereFields.Add(fieldValue);
                    }
                }
            }

            foreach (var node in wNode.GetNodes().ToList())
            {
                if (!node.ToString().Contains("{") && node.ToString().Contains(":") &&
                    node.ToString().Split(":").Length == 2)
                {
                    var column = node.ToString().Split(":");
                    if (!column[1].Contains("DESC") && !column[1].Contains("ASC") &&
                        clauseType.Contains(column[0]))
                    {
                        if (whereFields.Count == 0)
                        {
                            continue;
                        }

                        var clauseValue = column[1].Trim().Trim('"');
                        var fieldParts = whereFields.Last().Split('.');
                        var currentNodeTree = trees[currentEntity];
                        var field = fieldParts[1];

                        switch (column[0])
                        {
                            case "eq":
                                {
                                    var clause = SqlGraphQlHelper
                                        .ProcessFilter(currentNodeTree, linkEntityDictionaryTreeNode,
                                        field, "=",
                                        clauseValue, clauseCondition);
                                    AddToDictionary(sqlWhereStatement, clause, field, trees);
                                    break;
                                }
                            case "neq":
                                {
                                    var clause = SqlGraphQlHelper
                                        .ProcessFilter(currentNodeTree, linkEntityDictionaryTreeNode, field, "<>", clauseValue, clauseCondition);
                                    AddToDictionary(sqlWhereStatement, clause, field, trees);
                                    break;
                                }
                            case "in":
                                {
                                    clauseValue = "(" + string.Join(',',
                                        column[1].Replace("[", "").Replace("]", "").Split(',')
                                            .Select(v => $"'{v.Trim()}'")) + ")";
                                    var clause = SqlGraphQlHelper
                                        .ProcessFilter(currentNodeTree, linkEntityDictionaryTreeNode,
                                        field, "in", clauseValue, clauseCondition);
                                    AddToDictionary(sqlWhereStatement, clause, field, trees);
                                    break;
                                }
                        }

                        clauseCondition = string.Empty;
                    }
                }
            }

            GetFieldsWhere(trees, linkEntityDictionaryTreeNode, linkModelDictionaryTreeNode,
                whereFields,
                sqlWhereStatement,
                wNode,
                currentEntity, rootEntityName, wrapperEntityName, clauseCondition, clauseType, permission);
        }
    }
    
    private static void AddToDictionary(Dictionary<string, string> dictionary,
        List<string> values, string field, Dictionary<string, NodeTree> trees)
    {
        var entitiesWithColumn = trees.Values.Where(a => a.Mapping.Any(b => b.DestinationName.Matches(field))).ToList();

        foreach (var entity in entitiesWithColumn)
        {
            foreach (var value in values)
            {
                if (!dictionary.TryGetValue(entity.Name, out var _))
                {
                    dictionary.Add(entity.Name, value);
                }
                else
                {
                    dictionary[entity.Name] += " " + value;
                }
            }
        }
    }
}