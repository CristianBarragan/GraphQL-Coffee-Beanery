using System.Text;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using FASTER.core;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public class SqlSelectBuilder
{
    public static SqlStructure HandleGraphQL(
        ISelection rootSelection, 
        SqlCompilationContext context,
        Dictionary<string, SqlNode> sqlNodes,
        Dictionary<string, SqlNode> sqlNodeStatements,
        Dictionary<string, string> sqlWhereStatement,
        Dictionary<string, NodeTree> entityTrees,
        List<string> entityNames, NodeTree rootTree,
        IFasterKV<string, string> cache, string cacheKey,
        Dictionary<string, List<string>> permissions = null)
    {
        var sqlQueryStatement = new StringBuilder();
        var sqlQueryStructures = new Dictionary<string, SqlQueryStructure>(
            StringComparer.OrdinalIgnoreCase);
        var splitOnDapper = new Dictionary<string, Type>();
        var removeOnDapper = new Dictionary<string, Type>();
        var entityOrder = new List<string>();
        
        var childrenSqlStatement = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);

        var entityTypes = entityTrees.Select(a => a.Value.EntityType).ToList();
        
        GenerateQuery(entityTrees,
            entityTypes,
            sqlNodes,
            sqlQueryStatement, sqlNodeStatements, sqlWhereStatement,
            entityTrees.First(a => a.Key.Matches(rootTree.ModelToEntityLinks[0].AliasTo)).Value,
            childrenSqlStatement, entityNames, sqlQueryStructures,
            splitOnDapper, removeOnDapper, entityOrder, new List<string>());
        
        var queryStructure = sqlQueryStructures.FirstOrDefault();

        var sqlSelectStatement = queryStructure.Value.Query;

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
            var entity = rootTree.EntityType;
            splitOnDapperOrdered.Add(entity.Name, entity);
            entityMapping.Add(sqlQueryStructures.First().Key, entity);
        }

        if (string.IsNullOrEmpty(sqlSelectStatement))
        {
            return default;
        }

        return new SqlStructure()
        {
            SqlQuery = sqlSelectStatement,
            // Parameters = parameters,
            // SqlUpsert = sqlUpsertStatement,
            SplitOnDapper = splitOnDapperOrdered,
            // Pagination = pagination,
            HasTotalCount = false,
            EntityMapping = entityMapping
        };
    }
    
    public static void GetMutations(Dictionary<string, NodeTree> trees, ISyntaxNode node,
        Dictionary<string, SqlNode> linkModelDictionaryTree,
        Dictionary<string, SqlNode> linkEntityDictionaryTree,
        Dictionary<string, SqlNode> sqlStatementNodes, NodeTree currentTree,
        string previousNode, List<string> models)
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
                models);
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
        List<string> visitedModels,
        List<string> visitedEntities,
        List<string> models,
        Dictionary<string, SqlNode> modelSqlNodes,
        bool isEdge)
    {
        if (node != null && node.GetNodes()?.Count() == 0)
        {
            var fieldName = node.ToString().Trim();
            if (linkModelDictionaryTree.TryGetValue(
                    $"{currentTree.Alias}~{currentTree.Name}~{fieldName}",
                    out var sqlNodeFrom))
            {
                modelSqlNodes.Add($"{currentTree.Alias}~{currentTree.Name}~{fieldName}", sqlNodeFrom);
                
                var fieldMap = currentTree.NodeMap?.FieldMaps
                    .FirstOrDefault(f => f.SourceName.Equals(fieldName, 
                        StringComparison.OrdinalIgnoreCase));
                
                var destinationEntity = fieldMap?.DestinationEntity;

                if (!string.IsNullOrEmpty(destinationEntity) &&
                    entityTrees.TryGetValue(fieldMap.DestinationAlias, out var targetEntityTree))
                {
                    var entityNodeKey = $"{fieldMap.DestinationAlias}~{destinationEntity}~{fieldName.ToUpperCamelCase()}";
                    if (!linkEntityDictionaryTree.TryGetValue(entityNodeKey, out var sqlNodeTo))
                    {
                        entityNodeKey = $"{fieldMap.DestinationAlias}~{destinationEntity}~{fieldMap.DestinationName.ToUpperCamelCase()}";
                        linkEntityDictionaryTree.TryGetValue(entityNodeKey, out sqlNodeTo);
                    }

                    if (sqlNodeTo != null)
                    {
                        AddField(sqlStatementNodes,
                            $"{targetEntityTree.Alias}~{targetEntityTree.Name}~{fieldName.ToUpperCamelCase()}",
                            sqlNodeTo, isEdge);

                        if (!visitedModels.Contains(targetEntityTree.Alias))
                            visitedModels.Add(targetEntityTree.Alias);

                        visitedEntities.Add(sqlNodeFrom.Table);
                    }
                }
                else
                {
                    if (linkEntityDictionaryTree.TryGetValue(
                            sqlNodeFrom.RelationshipKey, out var sqlNodeTo))
                    {
                        var nodeEntityTree = sqlNodeFrom.LinkKeys.First(b => b.FromColumn.Matches(sqlNodeFrom.RelationshipKey.Split('~')[2]));
                        
                        var modelToEntityTree = entityTrees[nodeEntityTree.AliasTo];

                        AddField(sqlStatementNodes,
                            $"{modelToEntityTree.Alias}~{modelToEntityTree.Name}~{fieldName.ToUpperCamelCase()}",
                            sqlNodeTo, isEdge);

                        if (!visitedModels.Contains(currentTree.Alias))
                            visitedModels.Add(currentTree.Alias);
                    }

                    visitedEntities.Add(sqlNodeFrom.Table);
                }
            }

            return;
        }

        if (node == null) return;

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

            GetFields(trees, entityTrees, childNode,
                linkEntityDictionaryTree, linkModelDictionaryTree,
                sqlStatementNodes, currentTree, visitedModels, visitedEntities, models, modelSqlNodes, isEdge);
        }
    }

    private static void AddField(
        Dictionary<string, SqlNode> sqlStatementNodes,
        string key,
        SqlNode? sqlNode,
        bool isEdge)
    {		
		if (sqlNode == null) return;

        var cloned = sqlNode.Clone() as SqlNode;
        cloned!.SqlNodeTypes.Clear();
        cloned.SqlNodeTypes.Add(isEdge ? SqlNodeType.Edge : SqlNodeType.Node);
        
        sqlStatementNodes[key] = cloned;
    }
    
    private static void
        GenerateQuery(Dictionary<string, NodeTree> entityTrees,
            List<Type> entityTypes,
            Dictionary<string, SqlNode> linkEntityDictionaryTreeNode,
            StringBuilder sqlQueryStatement, Dictionary<string, SqlNode> sqlStatementNodes,
            Dictionary<string, string> sqlWhereStatement,
            NodeTree currentTree, Dictionary<string, string> childrenSqlStatement,
            List<string> entityNames,
            Dictionary<string, SqlQueryStructure> sqlQueryStructures,
            Dictionary<string, Type> splitOnDapper, Dictionary<string, Type> removeOnDapper, List<string> entityOrder, List<string> visitedEntities)
    {
        var hasChildren = false;
        var children = new List<string>();
        
        if (visitedEntities.Contains(currentTree.Alias))
        {
            return;
        }
        
        visitedEntities.Add(currentTree.Alias);

        var currentEntityStructure = GenerateEntityQuery(entityTrees,
            linkEntityDictionaryTreeNode,
            sqlStatementNodes, currentTree, sqlQueryStatement,
            sqlQueryStructures, sqlWhereStatement);
        
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
                splitOnDapper, removeOnDapper, entityOrder, visitedEntities);

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

            if (childStructure.Columns.Count == 0)
            {
                continue;
            }
            
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

        if (!string.IsNullOrEmpty(select))
        {
            if (sqlWhereStatement.TryGetValue(currentTree.Alias, out var currentSqlWhereStatementNode))
            {
                queryBuilder += " WHERE " + currentSqlWhereStatementNode.Replace("@","AND").Replace("$","OR").Replace("~",currentTree.Alias) + ")";
            }
            
            queryBuilder = queryBuilder.Replace("%", select);
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
        }
    }
    
    private static SqlQueryStructure GenerateEntityQuery(Dictionary<string, NodeTree> entityTrees,
        Dictionary<string, SqlNode> linkEntityDictionaryTreeNode,
        Dictionary<string, SqlNode> sqlStatementNodes, NodeTree currentTree, 
        StringBuilder sqlQueryStatement, Dictionary<string, SqlQueryStructure> sqlQueryStructures,
        Dictionary<string, string> sqlWhereStatement)
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
            .Where(k => k.Key.Split('~')[0].Matches(currentTree.Alias)).ToList());

        currentColumns = currentColumns.Distinct().ToList();

        if (currentColumns.Count > 1 && !currentColumns.First().Value.LinkKeys.Where(a => a.ToColumn.Matches("Id"))
                .Any(a => !sqlStatementNodes.Any(b => b.Value.Column.Matches(a.FromColumn))))
        {
            return new SqlQueryStructure();
        }

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
                
                foreach (var linkKey in Enumerable.Concat(currentTree.Parents, currentTree.RelatedParents).Where(a => a.ToColumn.Matches("Id")))
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
            
            joinOnKey = $"{currentTree.Alias}.\"{oneKey.FromColumn}\" AS \"{oneKey.FromColumn.ToSnakeCase(currentTree.Id)}\"";
            var joinOneKeyParent = $"~.\"{oneKey.FromColumn.ToSnakeCase(currentTree.Id)}\" AS \"{oneKey.FromColumn.ToSnakeCase(currentTree.Id)}\"";
            queryColumns.Add(joinOnKey);
            parentQueryColumns.Add(joinOneKeyParent); 
        }
        
        if (queryColumns.Count == 0)
        {
            return new SqlQueryStructure();
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
            ChildrenJoinColumns = childrenJoinColumns
        };

        return sqlStructure;
    }
    
    public static string GetFieldsOrdering(Dictionary<string, NodeTree> modelTrees,
        ISyntaxNode orderNode, NodeTree currentEntityTree, Dictionary<string, SqlNode> linkModelDictionaryTree)
    {
        var orderString = string.Empty;
        foreach (var oNode in orderNode.GetNodes())
        {
            var currentEntity = currentEntityTree.Name;
            if (oNode.ToString().Contains("{") && oNode.ToString()[0] != '{' &&
                oNode.ToString().Contains(":"))
            {
                currentEntity = oNode.ToString().Split(":")[0];
            }

            if (!oNode.ToString().Contains("{") && oNode.ToString().Contains(":"))
            {
                var column = oNode.ToString().Split(":");
                if ((column[1].Contains("DESC") || column[1].Contains("ASC")) &&
                    modelTrees.ContainsKey(currentEntity))
                {
                    var currentNodeTree = modelTrees[currentEntity];
                    orderString +=
                        SqlGraphQlHelper.HandleSort(currentNodeTree, column[0],
                            column[1], linkModelDictionaryTree);
                }
            }

            orderString +=
                $", {GetFieldsOrdering(modelTrees, oNode, currentEntityTree, linkModelDictionaryTree)}";
        }

        return orderString;
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