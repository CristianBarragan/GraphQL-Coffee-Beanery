using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlSelectBuilder
    {
        public static (string, Dictionary<string, Type>, List<string>) Build(
            NodeTree nodeTree,
            Dictionary<string, SqlNode> nodeDict,
            Dictionary<string, SqlNode> edgeDict,
            string wrapperEntityName,
            Dictionary<string, string> sqlWhereStatement,
            Dictionary<string, Type> splitOnDapper,
            Dictionary<string, string> aliases,
            bool transformedToParent)
        {
            var sqlQueryStructures = new Dictionary<string, SqlQueryStructure>(
                StringComparer.OrdinalIgnoreCase);
            var visitedEntities = new List<string>();
            var childrenSqlStatement = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);
            var entityOrder = new List<string>();

            foreach (var edgeKey in edgeDict)
            {
                if (!nodeDict.ContainsKey(edgeKey.Key))
                {
                    nodeDict.Add(edgeKey.Key, edgeKey.Value);    
                }
            }

            GenerateQuery(SqlNodeRegistry.EntityTrees,
                SqlNodeRegistry.EntityTypes,
                SqlNodeRegistry.EntityNodes, SqlNodeRegistry.ModelNodes,
                nodeDict, sqlWhereStatement,
                SqlNodeRegistry.EntityTrees[wrapperEntityName],
                childrenSqlStatement, wrapperEntityName, sqlQueryStructures, 
                splitOnDapper, aliases, entityOrder, visitedEntities, new List<string>());

            if (sqlQueryStructures.Count == 0)
            {
                return (string.Empty, default(Dictionary<string, Type>), default(List<string>));
            }
            
            var queryStructure = sqlQueryStructures.FirstOrDefault();

            if (transformedToParent && splitOnDapper.Count > 0)
            {
                var splitOn = splitOnDapper.FirstOrDefault(a => a.Value.Name == wrapperEntityName);

                if (splitOn.Value != null)
                {
                    splitOnDapper.Remove(splitOn.Key);
                }
                
                foreach (var childName in SqlNodeRegistry.EntityTrees[wrapperEntityName].Children)
                {
                    var childTree = SqlNodeRegistry.EntityTrees[childName];

                    if (sqlQueryStructures.Any(a => a.Value.Alias == childTree.Alias && a.Value.Visited))
                    {
                        continue;
                    }
                    
                    if (SqlNodeRegistry.ModelNodes.FirstOrDefault(a => a.Key.Split('~')[0].Matches(wrapperEntityName)).Value.RelationshipKey.Split('~')[1]
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

            var splitOnDapperOrdered = new Dictionary<string, Type>();
            var aliasesOrdered = new List<string>();

            foreach (var key in entityOrder)
            {
                var index = Array.IndexOf(splitOnDapper.Values.Select(a => a.Name).ToArray(), key);
                var kv = splitOnDapper.FirstOrDefault(t => t.Value.Name.Matches(key));

                if (kv.Value != null && !splitOnDapperOrdered.ContainsKey(kv.Key))
                {
                    splitOnDapperOrdered.Add(kv.Key, kv.Value);
                    aliasesOrdered.Add(aliases.ElementAtOrDefault(index).Value);
                }
            }

            if (splitOnDapperOrdered.Count == 0)
            {
                var entity = SqlNodeRegistry.EntityTypes[0] as Type;
                splitOnDapperOrdered.Add(entity.Name, entity);
                aliasesOrdered.Add(entity.Name);
            }
            
            return (sqlQueryStructures.LastOrDefault().Value.Query, splitOnDapperOrdered, aliasesOrdered);
        }
        
        private static void 
            GenerateQuery(Dictionary<string, NodeTree> entityTrees, List<Type> entityTypes, Dictionary<string,SqlNode> linkEntityDictionaryTree, 
                Dictionary<string,SqlNode> linkModelDictionaryTree, Dictionary<string, SqlNode> sqlStatementNodes, 
                Dictionary<string, string> sqlWhereStatement, NodeTree currentTree, Dictionary<string, string> childrenSqlStatement, string rootEntityName,
                Dictionary<string, SqlQueryStructure> sqlQueryStructures, Dictionary<string,Type> splitOnDapper, Dictionary<string, string> aliases, List<string> entityOrder, List<string> visitedEntities,
                List<string> generatedQueries)
        {
            var childrenOrder = new List<string>();
            var alias = currentTree.Alias;

            if (string.IsNullOrEmpty(currentTree.Alias))
            {
                alias = currentTree.Name;
            }
            
            if (visitedEntities.Contains(alias) || sqlQueryStructures.Any(a => a.Value.Alias == currentTree.Alias))
            {
                return;
            }
            
            visitedEntities.Add(alias);
            currentTree = entityTrees[alias];
            entityOrder.Add(currentTree.Name);

            foreach (var treeAlias in entityTrees.Where(a => a.Value.Name.Matches(currentTree.Name)))
            {
                if (sqlQueryStructures.Any(a => a.Value.Alias.Matches(treeAlias.Value.Alias)))
                {
                    continue;
                }
                
                foreach (var child in treeAlias.Value.Children.Where(c => !splitOnDapper.Keys.Contains(c)))
                {
                    if (treeAlias.Value.Children.Any(k => k.Matches(child)))
                    {
                        var childVisitedEntities = new List<string>(visitedEntities)
                        {
                            currentTree.Name
                        };

                        if (!string.IsNullOrEmpty(currentTree.ParentName))
                        {
                            childVisitedEntities.Add(currentTree.ParentName);
                        }
                        
                        GenerateQuery(entityTrees, entityTypes, linkEntityDictionaryTree, linkModelDictionaryTree, sqlStatementNodes, sqlWhereStatement,
                            entityTrees[child], childrenSqlStatement, rootEntityName, sqlQueryStructures, splitOnDapper, aliases, entityOrder, childVisitedEntities, generatedQueries);
                    }
                    childrenOrder.Add(child);
                }
            }

            var currentEntityStructure = GenerateEntityQuery(entityTrees, linkEntityDictionaryTree, sqlStatementNodes, currentTree,
                sqlQueryStructures, sqlWhereStatement, childrenSqlStatement, rootEntityName, generatedQueries);

            if (string.IsNullOrEmpty(currentEntityStructure.Query))
            {
                return;
            }

            var queryBuilder = $"SELECT % FROM \"{currentTree.Schema}\".\"{currentTree.Name}\" {currentTree.Alias} ";
            currentEntityStructure.SelectColumns.AddRange(currentEntityStructure.Columns);
            
            foreach (var child in currentTree.Children)
            {
                if (!string.IsNullOrEmpty(child) && sqlQueryStructures.ContainsKey(child) &&
                    !generatedQueries.Contains(sqlQueryStructures[child].Query)) 
                    // && !sqlQueryStructures[child].Visited)
                {
                    var isEntityInQuery = false;
                    var childStructure = sqlQueryStructures[child];
                    sqlQueryStructures[child].Visited = true;
                    generatedQueries.Add(sqlQueryStructures[child].Query);
                    
                    if (childStructure.SqlNode?.LinkKeys?.Count > 0)
                    {
                        var joinChildKey = String.Empty;
                        var joinParentKey = "\"Id\"";
                        var childAlias = childStructure.Alias;
                        
                        if (currentTree.Children.Count > 0)
                        {
                            foreach (var _ in sqlQueryStructures.Where(a => currentTree.Children.Any(b => b.Matches(a.Key))))
                            {
                                
                                joinChildKey = childStructure.Columns.FirstOrDefault(c => c.Contains($"\"Id\""));
            
                                if (string.IsNullOrEmpty(joinChildKey))
                                {
                                    joinChildKey =
                                        childStructure.Columns.FirstOrDefault(c => c.Contains($"\"Id\""));
                                }
            
                                if (string.IsNullOrEmpty(joinChildKey))
                                {
                                    joinChildKey = childStructure.Columns.FirstOrDefault(c => c.Contains($"\"Id"));
                                    joinParentKey = $"\"Id\"";
                                }
            
                                joinChildKey = joinChildKey.Split("AS").Last().Sanitize();
            
                                queryBuilder += childStructure.SqlNodeType == SqlNodeType.Edge ? " JOIN " : " LEFT JOIN ";
                                queryBuilder +=
                                    $" ( {childStructure.Query} ) {childAlias} ON {currentTree.Alias}.{joinParentKey} = {
                                        childAlias}.\"{joinChildKey}\"";
                                currentEntityStructure.SelectColumns.AddRange(
                                    childStructure.ParentColumns.Select(s => s.Replace("~", child)));
                                currentEntityStructure.ParentColumns.AddRange(childStructure.ParentColumns);
            
                                if (!splitOnDapper.ContainsKey("Id".ToSnakeCase(currentTree.Id)))
                                {
                                    splitOnDapper.Add("Id".ToSnakeCase(currentTree.Id),
                                        entityTypes.FirstOrDefault(e => e.Name.Matches(child)));
                                    aliases.Add(currentTree.Name, currentTree.Alias);
                                }
                            }
                        }
                        else
                        {
                            joinChildKey = childStructure.Columns.FirstOrDefault(c => c.Contains($"\"Id"));
                            joinParentKey = $"\"{childAlias}Id\"";
            
                            joinChildKey = joinChildKey.Split("AS").Last().Sanitize();
            
                            // queryBuilder += childStructure.SqlNodeType == SqlNodeType.Edge ? " JOIN " : " LEFT JOIN ";
                            // queryBuilder +=
                            //     $" ( {childStructure.Query} ) {childAlias} ON \"Id\" = {
                            //         childAlias}.\"{currentTree.Name}{"Id".ToSnakeCase(currentTree.Id)}\"";
                            currentEntityStructure.SelectColumns.AddRange(
                                childStructure.ParentColumns.Select(s => s.Replace("~", childAlias)));
                            currentEntityStructure.ParentColumns.AddRange(childStructure.ParentColumns);
            
                            if (!splitOnDapper.ContainsKey("Id".ToSnakeCase(currentTree.Id)))
                            {
                                splitOnDapper.Add("Id".ToSnakeCase(currentTree.Id),
                                    entityTypes.FirstOrDefault(e => e.Name.Matches(childAlias)));
                                aliases.Add(currentTree.Name, currentTree.Alias);
                            }
                        }
                    }
            
                    if (!splitOnDapper.ContainsKey("Id".ToSnakeCase(currentTree.Id)))
                    {
                        splitOnDapper.Add("Id".ToSnakeCase(currentTree.Id), entityTypes.FirstOrDefault(e => e.Name.Matches(currentTree.Name)));
                        aliases.Add(currentTree.Name, currentTree.Alias);
                    }
                }
            }
            
            var select = string.Join(",", currentEntityStructure.SelectColumns.Distinct());

            queryBuilder = queryBuilder.Replace("%", select);
            
            currentEntityStructure.Query = queryBuilder;
            var currentModelNode = linkModelDictionaryTree.FirstOrDefault(a => a.Key.Contains(currentTree.Name));
            var currentNode = linkEntityDictionaryTree.FirstOrDefault(a => a.Key.Contains(currentModelNode.Value.Table));

            if (currentNode.Key == null)
            {
                return;
            }
            
            currentEntityStructure.Id = currentTree.Id;
            currentEntityStructure.SqlNodeType = currentNode.Value.SqlNodeTypes.First();
            currentEntityStructure.SqlNode = currentNode.Value;
            currentEntityStructure.Columns = currentEntityStructure.Columns.Distinct().ToList();
            currentEntityStructure.ParentColumns = currentEntityStructure.ParentColumns.Distinct().ToList();
            currentEntityStructure.SelectColumns = currentEntityStructure.SelectColumns.Distinct().ToList();

            if (!sqlQueryStructures.TryGetValue(currentTree.Alias, out var sqlQueryStructure))
            {
                sqlQueryStructures.Add(currentTree.Alias, currentEntityStructure);
            }
        }


        /// <summary>
        /// Map each entity node into raw query SQL statement
        /// </summary>
        /// <param name="entityTrees"></param>
        /// <param name="sqlStatementNodes"></param>
        /// <param name="currentTree"></param>
        /// <param name="sqlQueryStatement"></param>
        /// <param name="sqlQueryStructures"></param>
        /// <param name="sqlWhereStatement"></param>
        /// <param name="childrenSqlStatement"></param>
        /// <param name="rootEntityName"></param>
        /// <returns></returns>
        private static SqlQueryStructure GenerateEntityQuery(Dictionary<string, NodeTree> entityTrees,
            Dictionary<string, SqlNode> linkEntityDictionaryTree,
            Dictionary<string, SqlNode> sqlStatementNodes, NodeTree currentTree,
            Dictionary<string, SqlQueryStructure> sqlQueryStructures, Dictionary<string, string> sqlWhereStatement,
            Dictionary<string, string> childrenSqlStatement, string rootEntityName, List<string> generatedQueries)
        {
            var currentColumns = new List<KeyValuePair<string, SqlNode>>();
            var childrenJoinColumns = new Dictionary<string, string>();

            currentColumns.Insert(0, new KeyValuePair<string, SqlNode>(
                linkEntityDictionaryTree.Keys.First(a => a.Contains($"{currentTree.Name}~Id")),
                linkEntityDictionaryTree
                    .FirstOrDefault(k => k.Key.Contains($"{currentTree.Alias}~{currentTree.Name}~Id")).Value
            ));
            
            currentColumns.AddRange(sqlStatementNodes
                .Where(k => k.Key.Split('~')[2].Matches(currentTree.Name) &&
                            !k.Value.LinkKeys.Any(b => b.From.Matches(k.Key)) 
                            // (currentTree.Mapping.Any(m => m.DestinationName.Matches(k.Key.Split('~')[1])) &&
                            //  !k.Key.Matches($"{currentTree.Name}Id"))).ToList()
            ));

            currentColumns[0].Value.SqlNodeTypes.Clear();
            currentColumns[0].Value.SqlNodeTypes.Add(SqlNodeType.Node);

            var existingSqlQueryStructure =
                sqlQueryStructures.Values.FirstOrDefault(b => b.Alias.Matches(currentTree.Name));

            if ((!currentTree.Children.Any(a => sqlQueryStructures.Values.Any(b => b.Alias.Matches(a))) && currentColumns.Count == 1) ||
                (existingSqlQueryStructure != null && (existingSqlQueryStructure.Visited)))
            {
                return new SqlQueryStructure();
            }
            
            var tableFieldParts = currentColumns.First().Key.Split('~');

            if (currentColumns.FirstOrDefault().Value != null)
            {
                foreach (var linkKey in currentColumns.FirstOrDefault().Value.LinkKeys)
                {
                    if (currentColumns.Any(c => c.Key.Matches($"{tableFieldParts[0]}~{tableFieldParts[1]}~{tableFieldParts[2]}~{linkKey.From.Split('~')[0]}Id")) ||
                        currentTree.Name.Matches($"{linkKey.From.Split('~')[0]}"))
                    {
                        continue;
                    }
                
                    var aux = currentColumns[0].Value;
                    aux.Column = $"{linkKey.From.Split('~')[0]}Id";
                    currentColumns.Add(new KeyValuePair<string, SqlNode>($"{tableFieldParts[0]}~{tableFieldParts[1]}~{tableFieldParts[2]}~{linkKey.From.Split('~')[0]}Id", aux));
                }
            }
            
            var queryBuilder = string.Empty;
            var queryColumns = new List<string>();
            var parentQueryColumns = new List<string>();
            
            foreach (var tableColumn in currentColumns)
            {
                tableFieldParts = tableColumn.Key.Split('~');
                queryColumns.Add($"{currentTree.Name}.\"{tableFieldParts[3]}\" AS \"{tableFieldParts[3].ToSnakeCase(currentTree.Id)}\"");
                parentQueryColumns.Add($"~.\"{tableFieldParts[3].ToSnakeCase(currentTree.Id)}\" AS \"{tableFieldParts[3].ToSnakeCase(currentTree.Id)}\"");
            }
            
            foreach (var childQuery in sqlQueryStructures.Where(c => 
                         currentTree.Children
                         .Any(b => b.Matches(c.Key)))) //&& !c.Value.Visited))
            {
                if (generatedQueries.Contains(sqlQueryStructures[childQuery.Key].Query))
                {
                    continue;
                }
                
                // generatedQueries.Add(sqlQueryStructures[childQuery.Key].Query);
                var childTree = entityTrees[childQuery.Key];
                // if (sqlQueryStructures.Values.Any(b => b.Alias.Matches(childTree.Alias) && b.Visited) ||
                //     sqlQueryStructures.Values.Any(b => b.Alias.Matches(currentTree.Alias) && b.Visited))
                // {
                //     continue;
                // }
                
                queryBuilder +=$" {(childQuery.Value.SqlNodeType == SqlNodeType.Edge ? " JOIN ( " : " LEFT JOIN  ( ") } {
                    childQuery.Value.Query
                }";
                
                childQuery.Value.Visited = true;
                var joinChildKey = $"\"{currentTree.ParentName}{"Id2".ToSnakeCase(childQuery.Value.Id)}\"";
                if (!childrenJoinColumns.ContainsKey($"{childQuery.Key.Split('~')[0]}"))
                {
                    childrenJoinColumns.Add($"{currentTree.Name}~{childQuery.Key.Split('~')[0]}",$"\"{currentTree.ParentName}{"Id".ToSnakeCase(childQuery.Value.Id)}\"");
                }
                
                if (childQuery.Value.SqlNode?.LinkKeys != null)
                {
                    var joinKeys = childQuery.Value.SqlNode.LinkKeys.Where(j => j.From.Matches(currentTree.Name)).ToList();
                
                    for (var i = 0; i < joinKeys.Count; i++)
                    {
                        if (i == 0)
                        {
                            queryBuilder +=
                                $" ) {childTree.Alias} ON {currentTree.Name}.\"{currentTree.Name}Id\" = {
                                    childTree.Alias}.{joinChildKey}";
                        }
                        else
                        {
                            queryBuilder +=
                                $" AND {childTree.Alias} ON {currentTree.Name}.\"{currentTree.Name}Id\" = {
                                    childTree.Alias}.{joinChildKey}";
                        }
                        queryColumns.Add($"{currentTree.Name}.\"{joinKeys[i].From}\" AS \"{joinKeys[i].From}\"");
                        queryColumns.Add($"{currentTree.Name}.\"{joinKeys[i].To}\" AS \"{joinKeys[i].To}\"");
                        parentQueryColumns.Add($"~.\"{joinKeys[i].From}\" AS \"{joinKeys[i].From}\"");
                        parentQueryColumns.Add($"~.\"{joinKeys[i].To}\" AS \"{joinKeys[i].To}\"");
                        currentColumns.Add(new KeyValuePair<string, SqlNode>(joinKeys[i].To, childQuery.Value.SqlNode));
                        currentColumns.Add(new KeyValuePair<string, SqlNode>(joinKeys[i].From, childQuery.Value.SqlNode));
                    }
                }
                
                if (currentColumns.Count > 0 && currentColumns[0].Value != null)
                {
                    var linkKeys = currentColumns[0].Value.LinkKeys.Where(k => k.To.Matches(childQuery.Key)).ToList();
                
                    if (linkKeys.Count > 0)
                    {
                        for (var i = 0; i < linkKeys.Count; i++)
                        {
                            if (i == 0)
                            {
                                queryBuilder +=
                                    $" ) {childTree.Alias} ON {currentTree.Alias}.\"Id\" = {
                                        childTree.Alias}.{joinChildKey}";
                            }
                            else
                            {
                                queryBuilder +=
                                    $" AND {childTree.Alias} ON {currentTree.Alias}.\"Id\" = {
                                        childTree.Alias}.{joinChildKey}";
                            }
                        } 
                    }
                }
            }

            if (currentColumns.Count <= 2 && childrenSqlStatement.Count > 0)
            {
                var newRootNodeTree = entityTrees[childrenSqlStatement.First().Key];
                sqlWhereStatement.TryGetValue(newRootNodeTree.Name, out var currentSqlWhereStatementNewRoot);
                var oldWhereStatement = currentSqlWhereStatementNewRoot;

                if (!string.IsNullOrEmpty(oldWhereStatement))
                {
                    oldWhereStatement = oldWhereStatement.Replace("~", newRootNodeTree.Name);

                    foreach (var field in oldWhereStatement.Split("\""))
                    {
                        if (currentColumns.Any(c => c.Value.Column.Matches(field)))
                        {
                            oldWhereStatement =
                                oldWhereStatement.Replace(field, $"{field.ToSnakeCase(newRootNodeTree.Id)}");
                        }
                    }

                    oldWhereStatement = $" WHERE {oldWhereStatement} ";
                }
                else
                {
                    oldWhereStatement = string.Empty;
                }
                
                currentSqlWhereStatementNewRoot = string.IsNullOrEmpty(currentSqlWhereStatementNewRoot) ? string.Empty : currentSqlWhereStatementNewRoot;

                if (childrenSqlStatement.Count > 0 && childrenSqlStatement.Count > 0 &&
                    !string.IsNullOrEmpty(currentSqlWhereStatementNewRoot))
                {
                    var cutoff = childrenSqlStatement.First().Value.IndexOf('(') + 1;
                    var sqlStatement =
                        $"{childrenSqlStatement.First().Value.Substring(cutoff, childrenSqlStatement.First()
                            .Value.Length - cutoff)}";
                    sqlStatement = sqlStatement.Replace(oldWhereStatement,
                        $" WHERE {currentSqlWhereStatementNewRoot.Replace("~", newRootNodeTree.Name)}");
                }
            }
            else
            {
                // queryBuilder = "";
                // queryBuilder += " SELECT % ";
                // queryBuilder += $" FROM \"{currentTree.Schema}\".\"{currentTree.Name}\" {currentTree.Alias}";

                sqlWhereStatement.TryGetValue(currentTree.Name, out var currentSqlWhereStatement);

                if (!string.IsNullOrEmpty(currentSqlWhereStatement))
                {
                    currentSqlWhereStatement = currentSqlWhereStatement.Replace("~", currentTree.Name);

                    foreach (var field in currentSqlWhereStatement.Split("\""))
                    {
                        if (currentColumns.Any(c => c.Value.Column.Matches(field)))
                        {
                            currentSqlWhereStatement =
                                currentSqlWhereStatement.Replace(field,
                                    $"{(currentTree.Name.Matches(rootEntityName) ? field : field.ToSnakeCase(currentTree.Id))}");
                        }
                    }

                    currentSqlWhereStatement = $" WHERE {currentSqlWhereStatement} ";
                }
                else
                {
                    currentSqlWhereStatement = string.Empty;
                }

                queryBuilder += $" {currentSqlWhereStatement}";
                queryBuilder.Insert(0, queryBuilder);
            }
            
            var select = string.Join(",", queryColumns.Select(s => $"{currentTree.Name}.\"{s}\" AS \"{s.ToSnakeCase(currentTree.Id)}\""));
            queryBuilder = queryBuilder.Replace("%", select);

            var sqlStructure = new SqlQueryStructure();

            // if (!sqlQueryStructures.Any(a => a.Value.Alias.Matches(currentTree.Alias)) &&
            //     !generatedQueries.Contains(queryBuilder))
            // {
                sqlStructure = new SqlQueryStructure()
                {
                    Id = currentTree.Id,
                    SqlNodeType = currentColumns.Count > 0 ? currentColumns.Last().Value.SqlNodeTypes.First() : SqlNodeType.Node,
                    SqlNode = currentColumns.Count > 0 ? currentColumns.Last().Value : new SqlNode(),
                    Query = queryBuilder,
                    Alias = currentTree.Alias,
                    Columns = queryColumns.Distinct().ToList(),
                    ParentColumns = parentQueryColumns.Distinct().ToList(),
                    ChildrenJoinColumns = childrenJoinColumns
                };
                sqlQueryStructures.Add(currentTree.Alias, sqlStructure);
            // }
            
            return sqlStructure;
        }
    }
}