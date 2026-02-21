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
        public static string Build(
            NodeTree nodeTree,
            Dictionary<string, SqlNode> nodeDict,
            Dictionary<string, SqlNode> edgeDict,
            string wrapperEntityName,
            Dictionary<string, string> sqlWhereStatement)
        {
            var sqlQueryStructures = new Dictionary<string, SqlQueryStructure>(
                StringComparer.OrdinalIgnoreCase);
            var splitOnDapper = new Dictionary<string, Type>();
            var visitedEntities = new List<string>();
            var childrenSqlStatement = new Dictionary<string, string>(
                StringComparer.OrdinalIgnoreCase);

            foreach (var edgeKey in edgeDict)
            {
                nodeDict.Add(edgeKey.Key, edgeKey.Value);
            }
            
            GenerateQuery(SqlNodeRegistry.EntityTrees,
                new List<Type>(),
                SqlNodeRegistry.EntityNodes, SqlNodeRegistry.ModelNodes,
                nodeDict, sqlWhereStatement,
                SqlNodeRegistry.EntityTrees[wrapperEntityName],
                childrenSqlStatement, wrapperEntityName, sqlQueryStructures,
                splitOnDapper, visitedEntities);
            
            return sqlQueryStructures.LastOrDefault().Value.Query;
            // var sb = new StringBuilder();
            // sb.Append("SELECT ");
            //
            // if (!ctx.SelectSqlFields.Any())
            //     sb.Append("* ");
            // else
            //     sb.Append(string.Join(", ", ctx.SelectSqlFields));
            //
            // sb.Append($" FROM \"{rootTree.Schema}\".\"{rootTree.Name}\" {rootTree.Name} ");
            //
            // // EDGE joins (INNER)
            // foreach (var edge in edgeDict.Values)
            // {
            //     if (string.IsNullOrEmpty(edge.RelationshipKey))
            //         continue;
            //
            //     sb.Append($"INNER JOIN \"{edge.Schema}\".\"{edge.RelationshipKey}\" {edge.RelationshipKey} " +
            //               $"ON {rootTree.Name}.\"{edge.JoinColumnFrom}\" = {edge.RelationshipKey}.\"{edge.JoinColumnTo}\" ");
            // }
            //
            // // NODE joins (LEFT)
            // foreach (var node in nodeDict.Values)
            // {
            //     if (string.IsNullOrEmpty(node.JoinTable))
            //         continue;
            //
            //     sb.Append($"LEFT JOIN \"{node.Schema}\".\"{node.JoinTable}\" {node.JoinTable} " +
            //               $"ON {rootTree.Name}.\"{node.JoinColumnFrom}\" = {node.JoinTable}.\"{node.JoinColumnTo}\" ");
            // }
            //
            // if (!string.IsNullOrEmpty(ctx.Where))
            //     sb.Append($" WHERE {ctx.Where}");
            //
            // if (!string.IsNullOrEmpty(ctx.OrderBy))
            //     sb.Append($" ORDER BY {ctx.OrderBy}");
            //
            // return sb.ToString();
        }
        
        private static void 
            GenerateQuery(Dictionary<string, NodeTree> entityTrees, List<Type> entityTypes, Dictionary<string,SqlNode> linkEntityDictionaryTree, 
                Dictionary<string,SqlNode> linkModelDictionaryTree, Dictionary<string, SqlNode> sqlStatementNodes, 
                Dictionary<string, string> sqlWhereStatement, NodeTree currentTree, Dictionary<string, string> childrenSqlStatement, string rootEntityName,
                Dictionary<string, SqlQueryStructure> sqlQueryStructures, Dictionary<string,Type> splitOnDapper, List<string> visitedEntities)
        {
            var childrenOrder = new List<string>();

            if (visitedEntities.Contains(currentTree.Name))
            {
                return;
            }
            
            visitedEntities.Add(currentTree.Name);
            currentTree = entityTrees[currentTree.Name];

            foreach (var child in currentTree.Children.Where(c => !splitOnDapper.Keys.Contains(c)))
            {
                if (currentTree.Children.Any(k => k.Matches(child)))
                {
                    GenerateQuery(entityTrees, entityTypes, linkEntityDictionaryTree, linkModelDictionaryTree, sqlStatementNodes, sqlWhereStatement,
                        entityTrees[child], childrenSqlStatement, rootEntityName, sqlQueryStructures, splitOnDapper, visitedEntities);
                }
                childrenOrder.Add(child);
            }

            var currentEntityStructure = GenerateEntityQuery(entityTrees, sqlStatementNodes, currentTree,
                sqlQueryStructures, sqlWhereStatement, childrenSqlStatement, rootEntityName);

            if (string.IsNullOrEmpty(currentEntityStructure.Query))
            {
                return;
            }

            var queryBuilder = $"SELECT % FROM \"{currentTree.Schema}\".\"{currentTree.Name}\" {currentTree.Name} ";
            currentEntityStructure.SelectColumns.AddRange(currentEntityStructure.Columns);
            
            foreach (var child in currentTree.Children)
            {
                if (!string.IsNullOrEmpty(child) && sqlQueryStructures.ContainsKey(child))
                {
                    var isEntityInQuery = false;
                    var childStructure = sqlQueryStructures[child];
                    if (childStructure.SqlNode?.LinkKeys?.Count > 0)
                    {
                        var joinChildKey = String.Empty;
                        var joinParentKey = "\"Id\"";

                        if (currentTree.Children.Count > 0)
                        {
                            foreach (var childName in currentTree.Children)
                            {
                                joinChildKey = childStructure.Columns.FirstOrDefault(c => c.Contains($"\"{childName}Id\""));

                                if (string.IsNullOrEmpty(joinChildKey))
                                {
                                    joinChildKey =
                                        childStructure.Columns.FirstOrDefault(c => c.Contains($"\"{currentTree.Name}Id\""));
                                }

                                if (string.IsNullOrEmpty(joinChildKey))
                                {
                                    joinChildKey = childStructure.Columns.FirstOrDefault(c => c.Contains($"\"Id"));
                                    joinParentKey = $"\"{child}Id\"";
                                }

                                joinChildKey = joinChildKey.Split("AS").Last().Sanitize();

                                queryBuilder += childStructure.SqlNodeType == SqlNodeType.Edge ? " JOIN " : " LEFT JOIN ";
                                queryBuilder +=
                                    $" ( {childStructure.Query} ) {child} ON {currentTree.Name}.{joinParentKey} = {
                                        child}.\"{joinChildKey}\"";
                                currentEntityStructure.SelectColumns.AddRange(
                                    childStructure.ParentColumns.Select(s => s.Replace("~", child)));
                                currentEntityStructure.ParentColumns.AddRange(childStructure.ParentColumns);

                                if (!splitOnDapper.ContainsKey("Id".ToSnakeCase(currentTree.Id)))
                                {
                                    splitOnDapper.Add("Id".ToSnakeCase(currentTree.Id),
                                        entityTypes.FirstOrDefault(e => e.Name.Matches(child)));
                                }
                            }
                        }
                        else
                        {
                            joinChildKey = childStructure.Columns.FirstOrDefault(c => c.Contains($"\"Id"));
                            joinParentKey = $"\"{child}Id\"";

                            joinChildKey = joinChildKey.Split("AS").Last().Sanitize();

                            queryBuilder += childStructure.SqlNodeType == SqlNodeType.Edge ? " JOIN " : " LEFT JOIN ";
                            queryBuilder +=
                                $" ( {childStructure.Query} ) {child} ON {currentTree.Name}.\"Id\" = {
                                    child}.\"{currentTree.Name}{"Id".ToSnakeCase(currentTree.Id)}\"";
                            currentEntityStructure.SelectColumns.AddRange(
                                childStructure.ParentColumns.Select(s => s.Replace("~", child)));
                            currentEntityStructure.ParentColumns.AddRange(childStructure.ParentColumns);

                            if (!splitOnDapper.ContainsKey("Id".ToSnakeCase(currentTree.Id)))
                            {
                                splitOnDapper.Add("Id".ToSnakeCase(currentTree.Id),
                                    entityTypes.FirstOrDefault(e => e.Name.Matches(child)));
                            }
                        }
                    }

                    if (!splitOnDapper.ContainsKey("Id".ToSnakeCase(currentTree.Id)))
                    {
                        splitOnDapper.Add("Id".ToSnakeCase(currentTree.Id), entityTypes.FirstOrDefault(e => e.Name.Matches(currentTree.Name)));    
                    }
                }
            }
            
            var select = string.Join(",", currentEntityStructure.SelectColumns);

            queryBuilder = queryBuilder.Replace("%", select);
            
            currentEntityStructure.Query = queryBuilder;
            var currentModelNode = linkModelDictionaryTree.FirstOrDefault(a => a.Key.Contains(currentTree.Name));
            var currentNode = linkEntityDictionaryTree.FirstOrDefault(a => a.Key.Contains(currentModelNode.Value.Table));

            if (currentNode.Key == null)
            {
                return;
            }
            
            currentEntityStructure.Id = currentTree.Id;
            currentEntityStructure.SqlNodeType = currentNode.Value.SqlNodeType;
            currentEntityStructure.SqlNode = currentNode.Value;

            if (sqlQueryStructures.TryGetValue(currentTree.Name, out var sqlQueryStructure))
            {
                sqlQueryStructures[currentTree.Name] = currentEntityStructure;
            }
            else
            {
                sqlQueryStructures.Add(currentTree.Name, currentEntityStructure);
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
            Dictionary<string, SqlNode> sqlStatementNodes, NodeTree currentTree, 
            Dictionary<string, SqlQueryStructure> sqlQueryStructures, Dictionary<string, string> sqlWhereStatement, 
            Dictionary<string, string> childrenSqlStatement, string rootEntityName)
        {
            var currentColumns = new List<KeyValuePair<string, SqlNode>>();
            var childrenJoinColumns = new Dictionary<string, string>();
            
            currentColumns.AddRange(sqlStatementNodes
                .Where(k => k.Key.Split('~')[0].Matches(currentTree.Name) &&
                            !k.Value.LinkKeys.Any(b => b.From.Matches(k.Key)) &&
                            (currentTree.Mapping.Any(m => m.DestinationName.Matches(k.Key.Split('~')[1])) &&
                             !k.Key.Matches($"{currentTree.Name}Id"))).ToList());

            if (currentColumns == null || currentColumns.Count == 0 || currentColumns[0].Key == null)
            {
                return new SqlQueryStructure();
            }

            // foreach (var joinKey in currentColumns.FirstOrDefault().Value.LinkKeys)
            // {
            //     if (currentColumns.Any(c => c.Key.Matches($"{currentTree.Name}~Id")))
            //     {
            //         continue;
            //     }
            //
            //     var aux = currentColumns[0].Value;
            //     aux.Column = $"{joinKey.To.Split('~')[0]}Id";
            //     currentColumns.Add(new KeyValuePair<string, SqlNode>($"{currentTree.Name}~{joinKey.To.Split('~')[0]}Id", aux));
            // }
            
            foreach (var linkKey in currentColumns.FirstOrDefault().Value.LinkKeys)
            {
                if (currentColumns.Any(c => c.Key.Matches($"{currentTree.Name}~{linkKey.From.Split('~')[0]}Id")) ||
                    currentTree.Name.Matches($"{linkKey.From.Split('~')[0]}"))
                {
                    continue;
                }
                
                var aux = currentColumns[0].Value;
                aux.Column = $"{linkKey.From.Split('~')[0]}Id";
                currentColumns.Add(new KeyValuePair<string, SqlNode>($"{currentTree.Name}~{linkKey.From.Split('~')[0]}Id", aux));
            }
            
            var queryBuilder = string.Empty;
            var queryColumns = new List<string>();
            var parentQueryColumns = new List<string>();
            
            foreach (var tableColumn in currentColumns)
            {
                var tableFieldParts = tableColumn.Key.Split('~');
                queryColumns.Add($"{currentTree.Name}.\"{tableFieldParts[1]}\" AS \"{tableFieldParts[1].ToSnakeCase(currentTree.Id)}\"");
                parentQueryColumns.Add($"~.\"{tableFieldParts[1].ToSnakeCase(currentTree.Id)}\" AS \"{tableFieldParts[1].ToSnakeCase(currentTree.Id)}\"");
            }
            
            foreach (var childQuery in sqlQueryStructures.Where(c => 
                         currentTree.Children
                         .Any(b => b.Matches(c.Key))))
            {
                queryBuilder +=$" {(childQuery.Value.SqlNodeType == SqlNodeType.Edge ? " JOIN ( " : " LEFT JOIN  ( ") } {
                    childQuery.Value.Query
                }";
                
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
                                $" ) {childQuery.Key} ON {currentTree.Name}.\"{currentTree.Name}Id\" = {
                                    childQuery.Key}.{joinChildKey}";
                        }
                        else
                        {
                            queryBuilder +=
                                $" AND {childQuery.Key} ON {currentTree.Name}.\"{currentTree.Name}Id\" = {
                                    childQuery.Key}.{joinChildKey}";
                        }
                        queryColumns.Add($"{currentTree.Name}.\"{joinKeys[i].From}\" AS \"{joinKeys[i].From}\"");
                        queryColumns.Add($"{currentTree.Name}.\"{joinKeys[i].To}\" AS \"{joinKeys[i].To}\"");
                        parentQueryColumns.Add($"~.\"{joinKeys[i].From}\" AS \"{joinKeys[i].From}\"");
                        parentQueryColumns.Add($"~.\"{joinKeys[i].To}\" AS \"{joinKeys[i].To}\"");
                        currentColumns.Add(new KeyValuePair<string, SqlNode>(joinKeys[i].To, childQuery.Value.SqlNode));
                        currentColumns.Add(new KeyValuePair<string, SqlNode>(joinKeys[i].From, childQuery.Value.SqlNode));
                    }
                }

                if (currentColumns.Count > 0)
                {
                    var linkKeys = currentColumns[0].Value.LinkKeys.Where(k => k.To.Matches(childQuery.Key)).ToList();
                    
                    if (linkKeys.Count == 0)
                    {
                        for (var i = 0; i < linkKeys.Count; i++)
                        {
                            if (i == 0)
                            {
                                queryBuilder +=
                                    $" ) {childQuery.Key} ON {currentTree.Name}.\"Id\" = {
                                        childQuery.Key}.{joinChildKey}";
                            }
                            else
                            {
                                queryBuilder +=
                                    $" AND {childQuery.Key} ON {currentTree.Name}.\"Id\" = {
                                        childQuery.Key}.{joinChildKey}";
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
                queryBuilder = "";
                queryBuilder += " SELECT % ";
                queryBuilder += $" FROM \"{currentTree.Schema}\".\"{currentTree.Name}\" {currentTree.Name}";

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

            var sqlStructure = new SqlQueryStructure()
            {
                Id = currentTree.Id,
                SqlNodeType = currentColumns.Count > 0 ? currentColumns.Last().Value.SqlNodeType : SqlNodeType.Node,
                SqlNode = currentColumns.Count > 0 ? currentColumns.Last().Value : new SqlNode(),
                Query = queryBuilder,
                Columns = queryColumns,
                ParentColumns = parentQueryColumns,
                ChildrenJoinColumns = childrenJoinColumns
            };

            if (!sqlQueryStructures.Any(a => a.Key.Matches(currentTree.Name)))
            {
                sqlQueryStructures.Add(currentTree.Name ,sqlStructure);    
            }
            
            return sqlStructure;
        }
    }
}