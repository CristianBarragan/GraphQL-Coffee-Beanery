using System.Text;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class SqlHelper
{
    /// <summary>
    /// Method for adding pagination into the query SQL statement 
    /// </summary>
    /// <param name="rootTree"></param>
    /// <param name="sqlQuery"></param>
    /// <param name="sqlOrderStatement"></param>
    /// <param name="pagination"></param>
    /// <param name="hasTotalCount"></param>
    public static string HandleQueryClause(NodeTree rootTree, string sqlQuery, string sqlOrderStatement,
        Pagination pagination, bool hasTotalCount = false)
    {
        var from = 1;
        var to = pagination!.PageSize;
        var sqlWhereStatement = string.Empty;

        if (!string.IsNullOrEmpty(pagination!.After) && pagination.First > 0 &&
            hasTotalCount)
        {
            from = int.Parse(pagination.After) + 1;
            to = from + pagination.First!.Value;
            sqlWhereStatement += string.IsNullOrEmpty(sqlWhereStatement)
                ? $" WHERE \"RowNumber\" BETWEEN {from} AND {to}"
                : $" AND \"RowNumber\" BETWEEN {from} AND {to}";
        }
        else if (!string.IsNullOrEmpty(pagination?.Before) && pagination.Last > 0 &&
                 hasTotalCount)
        {
            to = int.Parse(pagination.Before) - 1;
            from = to - pagination.Last!.Value;
            to = to >= 1 ? to : 1;
            from = from >= 1 ? from : 1;
            sqlWhereStatement += string.IsNullOrEmpty(sqlWhereStatement)
                ? $" WHERE \"RowNumber\" BETWEEN {from} AND {to}"
                : $" AND \"RowNumber\" BETWEEN {from} AND {to}";
        }
        else if (pagination!.First > 0 && pagination!.Last > 0 && hasTotalCount)
        {
            to = pagination!.First!.Value;
            sqlWhereStatement += string.IsNullOrEmpty(sqlWhereStatement)
                ? $" WHERE \"RowNumber\" BETWEEN {pagination!.First} AND {pagination!.Last}"
                : $" AND \"RowNumber\" BETWEEN {pagination!.First} AND {pagination!.Last}";
        }
        else if (pagination!.First > 0 && hasTotalCount)
        {
            to = pagination!.First!.Value;
            sqlWhereStatement += string.IsNullOrEmpty(sqlWhereStatement)
                ? $" WHERE \"RowNumber\" BETWEEN {pagination!.First} AND \"RowNumber\""
                : $" AND \"RowNumber\" BETWEEN {pagination!.First} AND \"RowNumber\"";
        }
        else if (pagination!.Last > 0 && hasTotalCount)
        {
            sqlWhereStatement += string.IsNullOrEmpty(sqlWhereStatement)
                ? $" WHERE \"RowNumber\" BETWEEN \"RowNumber\" - {pagination!.Last} AND \"RowNumber\""
                : $" AND \"RowNumber\" BETWEEN \"RowNumber\" - {pagination!.Last} AND \"RowNumber\"";
        }
        else
        {
            to = 0;
            from = 0;
        }

        var hasPagination = pagination.First > 0 || pagination.Last > 0 ||
                            (pagination.First > 0 &&
                             !string.IsNullOrEmpty(pagination.After)) ||
                            (pagination.Last > 0 &&
                             !string.IsNullOrEmpty(pagination.Before));
        var sql = $"WITH {rootTree.Schema}s AS (SELECT * FROM (SELECT * FROM (" + sqlQuery + $") {rootTree.Name} ) ";
        var totalCount = hasPagination && hasTotalCount
            ? $" DENSE_RANK() OVER({sqlOrderStatement}) AS \"RowNumber\","
            : "";
        sqlQuery = ($" {sql} a ) " +
                    $"SELECT * FROM ( SELECT (SELECT COUNT(DISTINCT \"{"Id".ToSnakeCase(rootTree.Id)}\") FROM {rootTree.Schema}s) \"RecordCount\", " +
                    $"{totalCount} * FROM {rootTree.Schema}s) a {sqlWhereStatement.Replace('~', 'a')}");
        return sqlQuery;
    }

    /// <summary>
    /// Generate upserts 
    /// </summary>
    /// <param name="trees"></param>
    /// <param name="sqlUpsertStatementNodes"></param>
    /// <param name="entityNames"></param>
    /// <param name="sqlWhereStatement"></param>
    /// <returns></returns>
    public static string GenerateUpsertStatements(Dictionary<string, NodeTree> trees,
        Dictionary<string, SqlNode> sqlNodes, string rootEntityName, string wrapperEntityName,
        List<string> generatedQuery, Dictionary<string, SqlNode> sqlUpsertStatementNodes, NodeTree currentTree,
        List<string> entityNames, Dictionary<string, string> sqlWhereStatement, List<string> entitiesProcessed,
        StringBuilder sqlUpsertBuilder, StringBuilder sqlSelectUpsertBuilder)
    {
        var sqlUpsert = string.Empty;

        if (entitiesProcessed.Contains(currentTree.Alias))
        {
            return string.Empty;
        }

        entitiesProcessed.Add(currentTree.Alias);
        
        var processingParentName = currentTree.Parents.Count == 0 ? currentTree.Alias : currentTree.Parents[0].To;
            
        var whereParentValue = sqlWhereStatement.GetValueOrDefault(processingParentName);
        var whereParentClause = string.Empty;
        if (!string.IsNullOrEmpty(whereParentValue))
        {
            whereParentClause = $" WHERE {whereParentValue.Replace("~", processingParentName)}";
        }

        var whereCurrentValue = sqlWhereStatement.GetValueOrDefault(currentTree.Alias);
        var whereCurrentClause = string.Empty;

        if (!string.IsNullOrEmpty(whereCurrentClause) && string.IsNullOrEmpty(whereParentClause))
        {
            whereCurrentClause = $" WHERE {whereCurrentValue.Replace("~", currentTree.Alias)}";
        }

        if (!string.IsNullOrEmpty(whereCurrentClause) && !string.IsNullOrEmpty(whereParentClause))
        {
            whereCurrentClause += $" {whereParentClause} {whereCurrentValue.Replace("~", currentTree.Alias)}";
        }

        var upsertingEntity = sqlUpsertStatementNodes.FirstOrDefault(s =>
            s.Key.Split('~')[0].Matches(currentTree.Alias) || !s.Value.LinkKeys
                .Any(a => a.To.Split('~')[0].Matches(processingParentName)));
        
        var sql = string.Empty;
        
        if (upsertingEntity.Value != null)
        {
            var currentColumns = sqlUpsertStatementNodes
                .Where(k => k.Key.Split('~')[0].Matches(currentTree.Alias) && 
                            entityNames.Contains(k.Value.RelationshipKey.Split('~')[1]) &&
                            ! k.Value.LinkKeys.Any(b => b.From.Matches(k.Key)) &&
                            ! k.Value.LinkKeys.Any(b => trees.Keys.Any(a => a.Matches(k.Key.Split('~')[2])))).ToList();

            if (currentColumns.Count == 0)
            {
                return sqlUpsert;
            }
            
            sql = GenerateUpsert(currentTree, trees, currentColumns, whereCurrentClause, entityNames, currentColumns.First().Value.UpsertKeys[0].Split('~')[1]);
            
            if (!string.IsNullOrEmpty(sql))
            {
                generatedQuery.Add(sql); 
                sqlUpsertBuilder.Append(generatedQuery.Last());
                sqlUpsertBuilder.Insert(0, " ; " + sql);
                sqlSelectUpsertBuilder.Insert(0, " ; " + GenerateSelectUpsert(currentTree, sqlNodes, entityNames,
                    trees, sqlUpsertStatementNodes, sqlWhereStatement, new List<string>(), rootEntityName, generatedQuery, 
                    wrapperEntityName));
            }
        }
        
        foreach (var childTree in trees.Where(t => 
                     entityNames.Contains(t.Key.Split('~')[0])))
        {
            GenerateUpsertStatements(trees, sqlNodes, rootEntityName, wrapperEntityName, generatedQuery,
                sqlUpsertStatementNodes, childTree.Value,
                entityNames, sqlWhereStatement, entitiesProcessed,
                sqlUpsertBuilder, sqlSelectUpsertBuilder);
        }
        
        return sqlUpsert;
    }

    /// <summary>
    /// Generate the main upsert without "Join columns [Ids]"
    /// </summary>
    /// <param name="currentTree"></param>
    /// <param name="trees"></param>
    /// <param name="sqlUpsertStatementNodes"></param>
    /// <param name="whereClause"></param>
    /// <returns></returns>
    public static string GenerateUpsert(NodeTree currentTree, Dictionary<string, NodeTree> trees,
        List<KeyValuePair<string, SqlNode>> currentColumns,
        string whereClause, List<string> entityNames, string onConflictKey)
    {
        var sqlUpsertAux = string.Empty;

        if (currentColumns.Count == 0)
        {
            return sqlUpsertAux;
        }

        sqlUpsertAux += $" INSERT INTO \"{currentTree.Schema}\".\"{currentTree.Name}\" ( " +
                        $" {string.Join(",", currentColumns.Select(s => $"\"{s.Value.RelationshipKey.Split('~')[2]}\"").ToList())}) VALUES ({
                            string.Join(",", currentColumns.Select(s => $"'{s.Value.Value}'").ToList())}) " +
                        $" ON CONFLICT" +
                        $" (\"{onConflictKey}\") ";

        var exclude = new List<string>();
        exclude.AddRange(
            currentColumns.Where(c => c.Value.UpsertKeys
                    .Any(u => !u.Matches(c.Value.RelationshipKey.Split('~')[1])))
                .Select(e => $"\"{e.Value.RelationshipKey.Split('~')[2]}\" = EXCLUDED.\"{e.Value.RelationshipKey.Split('~')[2]}\"")
        );

        if (exclude.Count > 0)
        {
            sqlUpsertAux += $" DO UPDATE SET {string.Join(",", exclude)} {whereClause};";
        }
        else
        {
            sqlUpsertAux += $" DO NOTHING {whereClause};";
        }

        return sqlUpsertAux;
    }

    /// <summary>
    /// Generate the upsert for "Join columns [Ids]"
    /// </summary>
    /// <param name="currentTree"></param>
    /// <param name="entityNames"></param>
    /// <param name="trees"></param>
    /// <param name="sqlUpsertStatementNodes"></param>
    /// <param name="whereClause"></param>
    /// <returns></returns>
    public static string GenerateSelectUpsert(NodeTree currentTree, Dictionary<string, SqlNode> sqlNodes, 
        List<string> entityNames,
        Dictionary<string, NodeTree> trees,
        Dictionary<string, SqlNode> sqlUpsertStatementNodes,
        Dictionary<string, string> sqlWhereStatement, List<string> entitiesProcessed, string rootEntityName,
        List<string> generatedQuery, string wrapperEntityName)
    {
        // if (entitiesProcessed.Contains(currentTree.Name))
        // {
        //     return string.Empty;
        // }
        //
        // entitiesProcessed.Add(currentTree.Name);
        
        var sqlUpsertAux = string.Empty;
        var hasUpsert = true;
        
        var currentColumns = sqlUpsertStatementNodes
            .Where(k => k.Key.Split('~')[0].Matches(currentTree.Alias) && 
                        entityNames.Contains(k.Value.RelationshipKey.Split('~')[1]) &&
                        ! k.Value.LinkKeys.Any(b => b.From.Matches(k.Key)) &&
                        ! k.Value.LinkKeys.Any(b => trees.Keys.Any(a => a.Matches(k.Key.Split('~')[2])))).ToList();

        if (currentColumns.Count == 0)
        {
            return sqlUpsertAux;
        }
        
        var columnsQuery = currentColumns.Where(c => c.Value.UpsertKeys.Any(k => 
                k.Matches($"{c.Value.RelationshipKey.Split('~')[1]}~{c.Value.RelationshipKey.Split('~')[2]}"))).ToList();

        var columnValue = columnsQuery.FirstOrDefault(a => a.Key.Split('~')[0]
                .Matches(currentTree.Alias)).Value;

        if (columnValue == null)
        {
            return sqlUpsertAux;
        }

        // var columns = columnsQuery.ToList();
        // var parentColumns = new List<KeyValuePair<string, SqlNode>>();
        // var childEntitiesFrom = new List<string>();
        // var childEntitiesFromColumn = new List<string>();
        //
        // foreach (var joinKey in Enumerable.Concat(columnValue.LinkKeys, columnValue.EntityRelatedChildren))
        // {
        //     // if (!joinKey.To.Matches(currentTree.Alias))
        //     // {
        //     //     columns.Add(new KeyValuePair<string, SqlNode>(joinKey.To, currentColumns.Last().Value));
        //     //     
        //     //     parentColumns = sqlUpsertStatementNodes
        //     //         .Where(k => trees[joinKey.To].Mapping.Any(f => f
        //     //                                                      .DestinationName.Matches(joinKey.ToColumn) &&
        //     //                                                  !entityNames.Any(e => e.Matches(joinKey.To)))).ToList();
        //     childEntitiesFrom.Add(joinKey.From);
        //     childEntitiesFromColumn.Add(joinKey.FromColumn);
        //         
        //         // sqlUpsertAux += GenerateCommand(columns, trees, currentTree, sqlWhereStatement, parentColumns, entityNames, joinKey.To.Split('~')[0]);
        //     // }
        // }
        
        // foreach (var joinOneKey in columnValue.EntityRelatedChildren)
        // {
        //     // if (!joinOneKey.From.Split('~')[0].Matches(currentTree.Name))
        //     // {
        //     //     columns = columnsQuery.ToList();
        //     //     columns.Add(new KeyValuePair<string, SqlNode>(joinOneKey.From, currentColumns.Last().Value));
        //     //     
        //     //     parentColumns = sqlUpsertStatementNodes
        //     //         .Where(k => trees[joinOneKey.From.Split('~')[0]]
        //     //             .Mapping.Any(f => f
        //     //             .DestinationName.Matches(k.Key.Split('~')[1]) &&
        //     //                 !entityNames.Any(e => e.Matches(k.Key.Split('~')[1])))).ToList();
        //         
        //     childEntitiesFrom.Add(joinOneKey.From);
        //     childEntitiesFrom.Add(joinOneKey.FromColumn);
        //         // sqlUpsertAux += GenerateCommand(columns, trees, currentTree, sqlWhereStatement, parentColumns, entityNames, joinOneKey.From.Split('~')[0]);
        //     // }
        // }

        foreach (var modelToEntity in currentTree.ModelToEntityLinks)
        {
            var columns = new List<KeyValuePair<string, SqlNode>>()
            {
                new(modelToEntity.ToColumn, (SqlNode)currentColumns.FirstOrDefault(a => a.Key.Split('~')[2].Matches(modelToEntity.FromColumn)).Value.Clone())
            };

            if (columns.Count == 0)
            {
                return sqlUpsertAux;
            }
            
            columns[0].Value.Table = modelToEntity.To;
            columns[0].Value.Column = modelToEntity.ToColumn;
            columns[0].Value.RelationshipKey = $"{modelToEntity.From}~{modelToEntity.To}~{modelToEntity.ToColumn}";
            
            sqlUpsertAux += GenerateUpsert(trees[modelToEntity.From], trees, columns,
                sqlWhereStatement.GetValueOrDefault(currentTree.Alias), entityNames, modelToEntity.ToColumn);
            
            sqlUpsertAux += " ; ";
            
            sqlUpsertAux += GenerateCommand(currentColumns, trees, currentTree, sqlWhereStatement, entityNames, modelToEntity.From, modelToEntity.FromColumn,
                modelToEntity.ToColumn);
            
            sqlUpsertAux += " ; ";
        }
        
        return sqlUpsertAux;
    }

    private static string GenerateCommand(List<KeyValuePair<string, SqlNode>> currentColumns, Dictionary<string, NodeTree> trees,
        NodeTree currentTree, Dictionary<string, string> sqlWhereStatement, 
        List<string> entityNames, string entity, string entityColumn, string toColumn)
    {
        
        // currentColumns = currentColumns.DistinctBy(a => a.Key).ToList();
        // parentColumns = parentColumns.DistinctBy(a => a.Key).ToList();
        
        var childTree = trees[entity];

        if (string.IsNullOrEmpty(entity))
        {
            return string.Empty;
        }
        
        var parentTree = trees[entity];

        var insertJoin = new List<string>()
        {
            $"\"{entity}Id\""
        };
        var selectJoin = new List<string>()
        {
            $"{entity}.\"Id\" AS" +
            $" \"{entity}Id\""
        };
        var excludeJoin = new List<string>()
        {
            $"\"{entity}Id\" = EXCLUDED.\"{entity}Id\""
        };

        var onConflictKey = currentColumns.FirstOrDefault(a => a.Value
            .UpsertKeys.Any(x => x.Matches($"{a.Value.RelationshipKey.Split('~')[1]}~{a.Value.RelationshipKey.Split('~')[2]}")) 
                                 && a.Value.RelationshipKey.Split('~')[0].Matches(currentTree.Alias));
        
        insertJoin.Add($"\"{onConflictKey.Value.Column}\"");
        selectJoin.Add($"'{onConflictKey.Value.Value}' AS \"{onConflictKey.Value.Column}\"");
        excludeJoin.Add($"\"{onConflictKey.Value.Column}\" = EXCLUDED.\"{onConflictKey.Value.Column}\"");
        
        insertJoin.AddRange(currentColumns.Where(a => !a.Key.Matches(onConflictKey.Key))
            .Select(a => $"\"{a.Value.Column}\""));
        selectJoin.AddRange(currentColumns.Where(a => !a.Key.Matches(onConflictKey.Key))
            .Select(a => $"'{a.Value.Value}' AS \"{a.Value.Column}\""));
        excludeJoin.AddRange(currentColumns.Where(a => !a.Key.Matches(onConflictKey.Key))
            .Select(a => $"\"{a.Value.Column}\" = EXCLUDED.\"{a.Value.Column}\""));

        var where = currentColumns.Where(a => a.Value.Column.Matches(entityColumn))
            .Select(s => $"\"{toColumn}\" = '{s.Value.Value}'").ToList();
        
        if (
            currentColumns.Count == 0 || 
            !currentColumns.Any(a => a.Value.UpsertKeys
                .Any(u => u.Matches($"{a.Value.RelationshipKey.Split('~')[1]}~{a.Value.RelationshipKey.Split('~')[2]}")) && a.Value.SqlNodeTypes.Any(a => a == SqlNodeType.Mutation) && 
                                     !string.IsNullOrEmpty(a.Value.Value)) ||
            where.Count == 0)
        {
            return string.Empty;
        }
        
        var sqlUpsertAux = $" ; INSERT INTO \"{currentTree.Schema}\".\"{currentTree.Name}\" ( " +
                            string.Join(",",insertJoin) +
                            $" ) ( SELECT {string.Join(",",selectJoin)}" + $" FROM \"{childTree.Schema}\".\"{childTree.Name}\" {entity} WHERE {
                               string.Join(" AND ",  where)}";

        sqlUpsertAux += $" ) ON CONFLICT" +
                        $" (\"{onConflictKey.Value.Column}\") ";

        if (excludeJoin.Count > 0)
        {
            sqlUpsertAux += $" DO UPDATE SET {string.Join(",", excludeJoin)}";
        }
        else
        {
            sqlUpsertAux += $" DO NOTHING";
        }
        
        return sqlUpsertAux;
    }
}