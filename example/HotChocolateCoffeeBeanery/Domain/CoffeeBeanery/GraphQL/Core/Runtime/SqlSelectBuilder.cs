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
        IFasterKV<string, string> cache, string cacheKey, string modelName, string wrapperName,
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
        var tranformedModel = string.Empty;

        while (string.IsNullOrEmpty(tranformedModel))
        {
            if (modelTrees.First(a => a.Value.ModelName.Matches(modelName)).Value.Parents.Count > 0)
            {
                var treeTransforming = modelTrees.First(a => a.Value.ModelName.Matches(modelName))
                    .Value;
                tranformedModel = treeTransforming.Parents[0].AliasFrom;
                rootEntityName = treeTransforming.Parents[0].AliasTo;
            }
            else
            {
                var treeTransforming = modelTrees.First(a => a.Value.ModelName.Matches(modelName))
                    .Value;
                tranformedModel = treeTransforming.ModelToEntityLinks[0].AliasFrom;
                rootEntityName = treeTransforming.ModelToEntityLinks[0].AliasTo;
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
            
            var mutationNodeToProcess = argument.GetNodes().Last(a => a.Kind == SyntaxKind.ObjectValue).GetNodes()
                .FirstOrDefault(a => !a.ToString().StartsWith("model") && !a.ToString().StartsWith("cacheKey"));

            if (mutationNodeToProcess != null)
            {
                var nodeTreeRoot = new NodeTree();
                nodeTreeRoot.Name = string.Empty;
                var statements = new List<string>();
                var selectStatements = new List<string>();
            
                if (mutationNodeToProcess.GetNodes().Last().ToString().StartsWith("["))
                {
                    foreach (var mutationNode in mutationNodeToProcess.GetNodes().Last().GetNodes())
                    {
                        GetMutations(modelTrees, mutationNode,
                            modelDictionary, entityDictionary,
                            sqlUpsertStatementNodes, modelTrees
                                .First(t =>
                                    t.Key.Matches(rootEntityName)).Value, string.Empty,
                            new NodeTree(), models, entityNames, visitedModels);
                        
                        SqlHelper.GenerateUpsertStatements(entityTrees, sqlUpsertStatementNodes, entityTrees[rootEntityName], entityNames,
                            sqlWhereStatement, new List<string>(), statements, selectStatements);
                    }
                }
                else
                {
                    GetMutations(modelTrees, mutationNodeToProcess,
                        modelDictionary, entityDictionary,
                        sqlUpsertStatementNodes, modelTrees
                            .First(t =>
                                t.Key.Matches(rootEntityName)).Value, string.Empty,
                        new NodeTree(), models, entityNames, visitedModels);
                    
                    SqlHelper.GenerateUpsertStatements(entityTrees, sqlUpsertStatementNodes, entityTrees[rootEntityName], entityNames,
                        sqlWhereStatement, new List<string>(), statements, selectStatements);
                }
                statements.Reverse();
                sqlUpsertStatement = string.Join(";", statements);
                sqlUpsertStatement += string.Join(";", selectStatements);
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
            GetFields(modelTrees, entityTrees, edgeNode.GetNodes()
                    .FirstOrDefault(a => a.Kind == SyntaxKind.SelectionSet).GetNodes().FirstOrDefault(),
                entityDictionary, modelDictionary, 
                sqlStatementNodes,
                entityTrees.First(t =>
                    t.Key.Matches(rootEntityName)).Value,
                new NodeTree(), visitedFieldModel, new List<string>(), models, entityNames,
                true);
        }
        
        visitedFieldModel.Clear();

        if (graphQlSelection.SelectionSet?.Selections!
                .FirstOrDefault(s => s.ToString().StartsWith("nodes")) != null)
        {
            GetFields(modelTrees, entityTrees, node,
                entityDictionary, modelDictionary,
                sqlStatementNodes,
                modelTrees.First(t =>
                    t.Key.Matches(rootEntityName)).Value,
                new NodeTree(), visitedFieldModel, new List<string>(), models, entityNames,
                false);
        }

        var sqlQueryStatement = new StringBuilder();
        var sqlQueryStructures = new Dictionary<string, SqlQueryStructure>(
            StringComparer.OrdinalIgnoreCase);
        var splitOnDapper = new Dictionary<string, Type>();
        var removeOnDapper = new Dictionary<string, Type>();
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
                entityTrees.First(a => a.Key.Matches(rootEntityName)).Value,
                childrenSqlStatement, entityNames, sqlQueryStructures,
                splitOnDapper, removeOnDapper, entityOrder, new List<string>(), rootEntityName);
            
            //if transformedToParent then will be used the first matching child, TODO: Support multiple child queries for complex entities

            var queryStructure = sqlQueryStructures.FirstOrDefault();

            // if (transformedToParent)
            // {
            //     splitOnDapper.Remove(splitOnDapper.First(a => a.Value.Name == transformedToParentName).Key);
            //     
            //     foreach (var childName in entityTrees[rootEntityName].Children.Select(a => a.To))
            //     {
            //         if (modelDictionary.FirstOrDefault(a => a.Key.Split('~')[0].Matches(tranformedModel)).Value.RelationshipKey.Split('~')[1]
            //             .Matches(childName))
            //         {
            //             queryStructure = sqlQueryStructures.FirstOrDefault(s => s.Key.Matches(childName));
            //             if (queryStructure.Value != null)
            //             {
            //                 break;
            //             }    
            //         }
            //         else
            //         {
            //             splitOnDapper.Remove(splitOnDapper.First(a => a.Value.Name == childName).Key);
            //         }
            //     }
            // }

            sqlSelectStatement = queryStructure.Value.Query;

            if (splitOnDapper.Count == 0)
            {
                splitOnDapper.Add(queryStructure.Value.JoinOnKey, entityTypes
                    .First(a => a.Name.Matches(queryStructure.Key)));
            }

            //Update cache
            // if (!isCached)
            // {
            //     cacheReadSession.Upsert(ref cacheKey, ref sqlStament);    
            // }
        }

        var splitOnDapperOrdered = new Dictionary<string, Type>();
        var entityMapping = new Dictionary<string, Type>();

        for (var i = 0; i < entityOrder.Count; i++)
        {
            var kv = splitOnDapper.FirstOrDefault(t => t.Value.Name.Matches(entityTrees[sqlQueryStructures.ElementAt(i).Key].Name));

            if (kv.Value != null && !splitOnDapperOrdered.ContainsKey(kv.Key) && !removeOnDapper.ContainsKey(kv.Key))
            {
                splitOnDapperOrdered.Add(kv.Key, kv.Value);
                entityMapping.Add(sqlQueryStructures.ElementAt(i).Key, kv.Value);
            }
        }

        if (splitOnDapperOrdered.Count == 0)
        {
            var entity = entityTrees[rootEntityName].EntityType;
            splitOnDapperOrdered.Add(entity.Name, entity);
            entityMapping.Add(sqlQueryStructures.First().Key, entity);
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
            HasTotalCount = false,
            EntityMapping = entityMapping
        };

        return sqlStructure;
    }
    
    public static void GetMutations(Dictionary<string, NodeTree> trees, ISyntaxNode node,
        Dictionary<string, SqlNode> linkModelDictionaryTree,
        Dictionary<string, SqlNode> linkEntityDictionaryTree,
        Dictionary<string, SqlNode> sqlStatementNodes, NodeTree currentTree,
        string previousNode, NodeTree parentTree, List<string> models, List<string> entities,
        List<string> visitedModels)
    {
        if (linkModelDictionaryTree.TryGetValue(
                $"{currentTree.Alias}~{currentTree.Name}~{node.ToString()}",
                out var sqlNodeFrom))
        {
            var isEnum = false;
            var enumIntValue = string.Empty;
        
            if (linkEntityDictionaryTree.TryGetValue(sqlNodeFrom.RelationshipKey,
                    out var sqlNodeTo))
            {
                if (previousNode.Split(':').Length == 2)
                {
                    var enumValue = sqlNodeTo.FromEnumeration.FirstOrDefault(a => a.Key.Matches(previousNode.Split(':')[1].Sanitize().Replace("_", "")));
                    if (!string.IsNullOrEmpty(enumValue.Key))
                    {
                        isEnum = true;
                        var toEnum = sqlNodeTo.FromEnumeration
                            .FirstOrDefault(e =>
                                e.Key.Matches(enumValue.Key)).Value;
                        sqlNodeTo.Value = toEnum.ToString();
                    }
                    else
                    {
                        sqlNodeTo.Value = previousNode.Split(':')[1].Sanitize();
                    }
                }

                AddEntity(linkEntityDictionaryTree, sqlStatementNodes, trees, currentTree,
                    sqlNodeTo, isEnum);
            }
        
            return;
        }

        if (node == null)
        {
            return;
        }

        foreach (var childNode in node.GetNodes())
        {
            var childName = node.GetNodes().FirstOrDefault(a => a.Kind == SyntaxKind.Name);
            
            if (childName != null && models.Any(e => e.Matches(childName.ToString())) ||
                childNode.ToString().Matches("nodes") ||
                childNode.ToString().Matches("node"))
            {
                if (childNode.ToString().Matches("nodes") || childNode.ToString().Matches("node"))
                    currentTree = trees.OrderBy(a => a.Value.Id).First().Value;
                else
                    currentTree = trees.First(a => a.Value.Name.Matches(childName.ToString())).Value;
            }
            else if (childName != null  && trees.FirstOrDefault(a => a.Value.ModelName.Matches(childName.ToString().ToUpperCamelCase())).Value != null)
            {
                currentTree = trees.FirstOrDefault(a => a.Value.ModelName.Matches(childName.ToString().ToUpperCamelCase())).Value;
            }

            GetMutations(trees, childNode, linkEntityDictionaryTree, linkModelDictionaryTree,
                sqlStatementNodes, currentTree, node.ToString(),
                parentTree, models, entities, visitedModels);
        }
    }
    
    private static void AddEntity(Dictionary<string, SqlNode> linkEntityDictionaryTree,
        Dictionary<string, SqlNode> sqlStatementNodes, Dictionary<string, NodeTree> entityTrees, NodeTree currentTree,
        SqlNode? sqlNode, bool isEnum)
    {
        
        foreach (var auxFieldMap in currentTree.NodeMap?.FieldMaps
                     .Where(f => f.SourceName.Matches(sqlNode.RelationshipKey.Split('~')[2])))
        {
            var entity = sqlNode.Clone() as SqlNode;
            
            if (string.IsNullOrEmpty(entity.Value))
            {
                entity.Value = sqlNode.Value;
            }
            
            entity.Alias = auxFieldMap.DestinationAlias;
            entity.Table = (currentTree.IsEntity ? currentTree.Name : auxFieldMap.DestinationEntity);
            entity.RelationshipKey =
                $"{(currentTree.IsEntity ? currentTree.Alias : auxFieldMap.DestinationAlias)}~{
                    (currentTree.IsEntity ? currentTree.Name : auxFieldMap.DestinationEntity)}~{
                        sqlNode.RelationshipKey.Split('~')[2]}";
            entity.FromComplexModel = !currentTree.IsEntity;
            
            if (!sqlStatementNodes.ContainsKey(entity.RelationshipKey))
            {
                sqlStatementNodes.Add(entity.RelationshipKey, entity);
            }
        }
    }
    
    public static void GetFields(
        Dictionary<string, NodeTree> trees,
        Dictionary<string, NodeTree> entityTrees,
        ISyntaxNode node,
        Dictionary<string, SqlNode> linkEntityDictionaryTree,
        Dictionary<string, SqlNode> linkModelDictionaryTree,
        Dictionary<string, SqlNode> sqlStatementNodes,
        NodeTree currentTree,
        NodeTree parentTree,
        List<string> visitedModels,
        List<string> visitedEntities,
        List<string> models,
        List<string> entities,
        bool isEdge)
    {
        // Switch tree if this node's name directly matches a registered model alias
        if (trees.TryGetValue(node.ToString().Split('{')[0].Trim(), out var namedTree))
            currentTree = namedTree;

        if (node != null && node.GetNodes()?.Count() == 0)
        {
            var fieldName = node.ToString().Trim();
            var lookupKey = $"{currentTree.Alias}~{currentTree.Name}~{fieldName}";
            var exists = linkModelDictionaryTree.ContainsKey(lookupKey);
            Console.WriteLine($"[GetFields] leaf={fieldName} tree={currentTree.Alias} key={lookupKey} exists={exists}");

            // ── Primary lookup: model node → entity node ──────────────────────────
            if (linkModelDictionaryTree.TryGetValue(
                    $"{currentTree.Alias}~{currentTree.Name}~{fieldName}",
                    out var sqlNodeFrom))
            {
                // Find the FieldMap for this field to get the correct DestinationEntity
                var fieldMap = currentTree.NodeMap?.FieldMaps
                    .FirstOrDefault(f => f.SourceName.Equals(fieldName, 
                        StringComparison.OrdinalIgnoreCase));
                
                var destinationEntity = fieldMap?.DestinationEntity;

                if (!string.IsNullOrEmpty(destinationEntity) &&
                    entityTrees.TryGetValue(fieldMap.DestinationAlias, out var targetEntityTree))
                {
                    // Look up the entity node directly by destination entity
                    var entityNodeKey = $"{fieldMap.DestinationAlias}~{destinationEntity}~{fieldName.ToUpperCamelCase()}";
                    if (!linkEntityDictionaryTree.TryGetValue(entityNodeKey, out var sqlNodeTo))
                    {
                        // Try with DestinationName instead of fieldName
                        entityNodeKey = $"{fieldMap.DestinationAlias}~{destinationEntity}~{fieldMap.DestinationName.ToUpperCamelCase()}";
                        linkEntityDictionaryTree.TryGetValue(entityNodeKey, out sqlNodeTo);
                    }

                    if (sqlNodeTo != null)
                    {
                        AddField(linkEntityDictionaryTree, sqlStatementNodes,
                            targetEntityTree,
                            $"{targetEntityTree.Alias}~{targetEntityTree.Name}~{fieldName.ToUpperCamelCase()}",
                            $"{targetEntityTree.Alias}~{targetEntityTree.Name}~{fieldName.ToUpperCamelCase()}",
                            sqlNodeTo, isEdge);

                        if (!visitedModels.Contains(targetEntityTree.Alias))
                            visitedModels.Add(targetEntityTree.Alias);

                        visitedEntities.Add(sqlNodeFrom.Table);
                    }
                }
                else
                {
                    // Original path for direct entity mappings (non-model-only types)
                    if (linkEntityDictionaryTree.TryGetValue(
                            sqlNodeFrom.RelationshipKey, out var sqlNodeTo))
                    {
                        var nodeEntityTree = sqlNodeFrom.LinkKeys.First(b => b.FromColumn.Matches(sqlNodeFrom.RelationshipKey.Split('~')[2]));
                        
                        var modelToEntityTree = entityTrees[nodeEntityTree.AliasTo];

                        AddField(linkEntityDictionaryTree, sqlStatementNodes,
                            modelToEntityTree,
                            $"{modelToEntityTree.Alias}~{modelToEntityTree.Name}~{fieldName.ToUpperCamelCase()}",
                            $"{modelToEntityTree.Alias}~{modelToEntityTree.Name}~{fieldName.ToUpperCamelCase()}",
                            sqlNodeTo, isEdge);

                        if (!visitedModels.Contains(currentTree.Alias))
                            visitedModels.Add(currentTree.Alias);
                    }

                    visitedEntities.Add(sqlNodeFrom.Table);
                }
                
                return;
            }

            return;
        }

        if (node == null) return;

        foreach (var childNode in node.GetNodes())
        {
            var childName = node.ToString().Split('{')[0].Trim();

            if (models.Any(e => e.Matches(childName)) ||
                childNode.ToString().Matches("nodes") ||
                childNode.ToString().Matches("node"))
            {
                if (childNode.ToString().Matches("nodes") || childNode.ToString().Matches("node"))
                    currentTree = trees.OrderBy(a => a.Value.Id).First().Value;
                else
                    currentTree = trees.First(a => a.Value.Name.Matches(childName)).Value;
            }
            else if (currentTree.NodeMap?.ModelChildren.Any(l => l.To.Matches(childName)) == true)
            {
                currentTree = trees.First(a => a.Value.ModelName.Matches(childName)).Value;
            }

            GetFields(trees, entityTrees, childNode,
                linkEntityDictionaryTree, linkModelDictionaryTree,
                sqlStatementNodes, currentTree, parentTree,
                visitedModels, visitedEntities, models, entities, isEdge);
        }
    }

    private static void AddField(
        Dictionary<string, SqlNode> linkEntityDictionaryTree,
        Dictionary<string, SqlNode> sqlStatementNodes,
        NodeTree currentTree,
        string key,
        string keyTo,
        SqlNode? sqlNode,
        bool isEdge)
    {
        if (sqlNode == null) return;
        Console.WriteLine($"[AddField] key={key} column={sqlNode.Column} relationshipKey={sqlNode.RelationshipKey}");

        var cloned = sqlNode.Clone() as SqlNode;
        cloned!.SqlNodeTypes.Clear();
        cloned.SqlNodeTypes.Add(isEdge ? SqlNodeType.Edge : SqlNodeType.Node);

        // Register under the explicit key so GenerateEntityQuery can find it
        // by alias prefix (key.Split('~')[0] == currentTree.Alias)
        if (sqlStatementNodes.ContainsKey(key))
            sqlStatementNodes[key] = cloned;
        else
            sqlStatementNodes.Add(key, cloned);

        // Also update the source node's type in the entity dictionary
        if (linkEntityDictionaryTree.TryGetValue(key, out var existing))
        {
            existing.SqlNodeTypes.Clear();
            existing.SqlNodeTypes.Add(isEdge ? SqlNodeType.Edge : SqlNodeType.Node);
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
            Dictionary<string, Type> splitOnDapper, Dictionary<string, Type> removeOnDapper, List<string> entityOrder, List<string> visitedEntities, string rootNodeTree)
    {
        var hasChildren = false;
        var children = new List<string>();
        
        if (visitedEntities.Contains(currentTree.Alias))
        {
            return true;
        }
        
        visitedEntities.Add(currentTree.Alias);
        
        var currentEntityStructure = GenerateEntityQuery(entityTrees,
            linkEntityDictionaryTreeNode,
            sqlStatementNodes, currentTree, entityNames, sqlQueryStatement,
            sqlQueryStructures, sqlWhereStatement, childrenSqlStatement, rootNodeTree, visitedEntities, hasChildren);
        
        if (!sqlQueryStructures.Any(a => a.Key
                .Matches(currentTree.Alias)))
        {
            sqlQueryStructures.Add(currentTree.Alias, currentEntityStructure);
        }
        
        var queryBuilder = $"SELECT % FROM \"{currentTree.Schema}\".\"{currentTree.Name}\" {currentTree.Alias} ";
        currentEntityStructure.SelectColumns.AddRange(currentEntityStructure.Columns);
        currentEntityStructure.SelectColumns = currentEntityStructure.SelectColumns.Distinct().ToList();
        currentEntityStructure.HasChildren = true;
        
        currentTree = entityTrees[currentTree.Alias];

        if (!entityOrder.Contains(currentTree.Name))
        {
            entityOrder.Add(currentTree.Name);    
        }
        
        foreach (var child in currentTree.Children.Concat(currentTree.RelatedChildren).DistinctBy(a => a.AliasTo))
        {
            var childTree = entityTrees[child.AliasTo];
            
            GenerateQuery(entityTrees, entityTypes, linkEntityDictionaryTreeNode,
                sqlQueryStatement, sqlStatementNodes, sqlWhereStatement,
                childTree, childrenSqlStatement, entityNames, sqlQueryStructures,
                splitOnDapper, removeOnDapper, entityOrder, visitedEntities, rootNodeTree);

            if (!(sqlQueryStructures.Any(a => a.Key.Matches(childTree.Alias) &&
                                              sqlQueryStructures[childTree.Alias].Columns.ToList().Count - 1 > sqlQueryStructures[childTree.Alias].ChildrenJoinColumns.Count)) &&
                  !entityTrees.Any(b => b.Value.Parents.Any(c => c.AliasTo.Matches(childTree.Alias))))
            {
                if (!removeOnDapper.ContainsKey("Id".ToSnakeCase(childTree.Id)))
                {
                    removeOnDapper.Add("Id".ToSnakeCase(childTree.Id),
                        entityTypes.FirstOrDefault(e => e.Name.Matches(childTree.Name)));
                }
                continue;
            }

            children.Add(childTree.Alias);
            
            var childStructure = sqlQueryStructures[childTree.Alias];
            childStructure.HasChildren = true;
            queryBuilder += childStructure.SqlNodeType == SqlNodeType.Edge ? " JOIN " : " LEFT JOIN ";
            var linkToChild = childTree.Parents
                .FirstOrDefault(a => a.To.Matches(currentTree.Name));

            if (linkToChild == null)
            {
                linkToChild = childTree.RelatedParents
                    .FirstOrDefault(a => a.To.Matches(currentTree.Name));
            }
            
            queryBuilder +=
                $" ( {childStructure.Query} ) {childTree.Alias} ON {currentTree.Alias}.\"{
                    linkToChild.ToColumn}\" = {childTree.Alias}.\"{linkToChild.FromColumn.ToSnakeCase(childTree.Id)}\"";

            currentEntityStructure.SelectColumns.AddRange(
                childStructure.ParentColumns.Select(s => s.Replace("~", childTree.Alias)));
            currentEntityStructure.ParentColumns.AddRange(childStructure.ParentColumns);

            currentEntityStructure.SelectColumns = currentEntityStructure.SelectColumns.Distinct().ToList();
            currentEntityStructure.ParentColumns = currentEntityStructure.ParentColumns.Distinct().ToList();
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
        string rootEntity, List<string> visitedEntities, bool hasChildren)
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
                        ! k.Value.LinkKeys.Any(b => b.From.Matches(k.Key)) &&
                        ! k.Value.LinkKeys.Any(b => entityTrees.Keys.Any(a => a.Matches(k.Key.Split('~')[2])))).ToList());

        currentColumns = currentColumns.Distinct().ToList();

        var queryBuilder = string.Empty;
        var queryColumns = new List<string>();
        var parentQueryColumns = new List<string>();

        foreach (var tableColumn in currentColumns)
        {
            var fieldName = tableColumn.Value.Column;

            if (!queryColumns.Contains($"{currentTree.Alias}.\"{fieldName}\" AS \"{fieldName
                .ToSnakeCase(currentTree.Id)}\""))
            {
                queryColumns.Add(
                    $"{currentTree.Alias}.\"{fieldName}\" AS \"{fieldName
                        .ToSnakeCase(currentTree.Id)}\"");
                
                parentQueryColumns.Add(
                    $"~.\"{fieldName.ToSnakeCase(currentTree.Id)}\" AS \"{fieldName
                        .ToSnakeCase(currentTree.Id)}\"");
                
                foreach (var linkKey in Enumerable.Concat(currentTree.Parents, currentTree.RelatedParents))
                {
                    if (!sqlQueryStructures.Any(s => s.Key.Matches(linkKey.AliasTo)))
                    {
                        continue;
                    }

                    if (childrenJoinColumns.ContainsKey(linkKey.AliasTo))
                    {
                        continue;    
                    }
                    
                    childrenJoinColumns.Add(linkKey.AliasTo, linkKey.ToColumn);
                    
                    if (!queryColumns.Contains($"{currentTree.Alias}.\"{linkKey.FromColumn}\" AS \"{linkKey.FromColumn
                        .ToSnakeCase(currentTree.Id)}\""))
                    {
                        queryColumns.Add(
                            $"{currentTree.Alias}.\"{linkKey.FromColumn}\" AS \"{linkKey.FromColumn
                                .ToSnakeCase(currentTree.Id)}\"");
                    }
                    
                    if (!parentQueryColumns.Contains($"~.\"{linkKey.FromColumn.ToSnakeCase(currentTree.Id)}\" AS \"{linkKey.FromColumn
                        .ToSnakeCase(currentTree.Id)}\""))
                    {
                        parentQueryColumns.Add(
                            $"~.\"{linkKey.FromColumn.ToSnakeCase(currentTree.Id)}\" AS \"{linkKey.FromColumn
                                .ToSnakeCase(currentTree.Id)}\"");
                    }
                }
            }
        }
        
        var joinOnKey = string.Empty;
        var currentJoinOnKeys = currentColumns.FirstOrDefault(a => a.Key.Split('~')[0].Matches(currentTree.Alias)).Value;
        
        var oneKey = currentJoinOnKeys.EntityRelatedChildren.FirstOrDefault();

        if (oneKey == null)
        {
            oneKey = currentJoinOnKeys.EntityChildren.FirstOrDefault();
        }

        if (oneKey != null && !childrenJoinColumns.ContainsKey(currentJoinOnKeys.RelationshipKey.Split('~')[0]))
        {
            if (!childrenJoinColumns.ContainsValue(oneKey.ToColumn))
            {
                childrenJoinColumns.Add(currentJoinOnKeys.RelationshipKey.Split('~')[0], oneKey.ToColumn);    
            }
            
            joinOnKey = $"{currentTree.Alias}.\"Id\" AS \"{oneKey.To}{oneKey
                .ToColumn.ToSnakeCase(currentTree.Id)}\"";
            var joinOneKeyParent = $"~.\"{"Id".ToSnakeCase(currentTree.Id)}\" AS \"{oneKey.ToColumn.ToSnakeCase(currentTree.Id)}\"";
            queryColumns.Add(joinOnKey);
            parentQueryColumns.Add(joinOneKeyParent);
            joinOnKey = $"{oneKey.ToColumn.ToSnakeCase(currentTree.Id)}";    
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

        queryColumns = queryColumns.Distinct().ToList();
        var select = string.Join(",", queryColumns);
        queryBuilder = queryBuilder.Replace("%", select);

        var sqlStructure = new SqlQueryStructure()
        {
            Id = currentTree.Id,
            Name = currentTree.Name,
            SqlNodeType = currentColumns.Count > 1 && currentColumns.Last().Value.SqlNodeTypes.Count > 0 ? currentColumns.Last().Value.SqlNodeTypes[0] : SqlNodeType.Node,
            SqlNode = currentColumns.Count > 0 ? currentColumns.Last().Value :
                new SqlNode(),
            Query = queryBuilder,
            Columns = queryColumns,
            ParentColumns = parentQueryColumns,
            ChildrenJoinColumns = childrenJoinColumns,
            WhereClause = entitySqlWhereStatement,
            JoinOnKey = joinOnKey
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