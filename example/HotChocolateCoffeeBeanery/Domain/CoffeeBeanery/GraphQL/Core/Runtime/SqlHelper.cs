using System.Text;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using MoreLinq;

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

    public static void GenerateUpsertStatements(
        Dictionary<string, NodeTree> trees,
        Dictionary<string, SqlNode> sqlUpsertStatementNodes,
        NodeTree currentTree,
        List<string> entityNames,
        Dictionary<string, string> sqlWhereStatement,
        List<string> entitiesProcessed,
        List<string> statements,
        List<string> selectStatements)
    {
        if (entitiesProcessed.Contains(currentTree.Alias))
            return;

        entitiesProcessed.Add(currentTree.Alias);

        var processingParentName = currentTree.Parents.Count == 0
            ? currentTree.Alias
            : currentTree.Parents[0].To;

        var whereParentValue  = sqlWhereStatement.GetValueOrDefault(processingParentName);
        var whereParentClause = string.Empty;
        if (!string.IsNullOrEmpty(whereParentValue))
            whereParentClause = $" WHERE {whereParentValue.Replace("~", processingParentName)}";

        var whereCurrentValue  = sqlWhereStatement.GetValueOrDefault(currentTree.Alias);
        var whereCurrentClause = string.Empty;

        if (!string.IsNullOrEmpty(whereCurrentValue) && string.IsNullOrEmpty(whereParentClause))
            whereCurrentClause = $" WHERE {whereCurrentValue.Replace("~", currentTree.Alias)}";

        if (!string.IsNullOrEmpty(whereCurrentValue) && !string.IsNullOrEmpty(whereParentClause))
            whereCurrentClause = $"{whereParentClause} AND {whereCurrentValue.Replace("~", currentTree.Alias)}";

        var currentColumns = sqlUpsertStatementNodes
            .Where(k =>
                k.Key.Split('~')[0].Matches(currentTree.Alias) &&
                entityNames.Contains(k.Value.RelationshipKey.Split('~')[1]) &&
                !k.Value.LinkKeys.Any(b => b.From.Matches(k.Key)) &&
                !k.Value.LinkKeys.Any(b =>
                    trees.Keys.Any(a => a.Matches(k.Key.Split('~')[2]))))
            .ToList();

        if (currentColumns.Count > 0)
        {
            GenerateSelectUpsert(
                currentTree, entityNames, trees, currentColumns,
                sqlUpsertStatementNodes, sqlWhereStatement, statements, selectStatements);
        }
        
        var allChildLinks = currentTree.Children
            .Concat(currentTree.RelatedChildren)
            .ToList();

        if (currentTree.NodeMap?.ModelChildren != null)
        {
            allChildLinks.AddRange(currentTree.NodeMap.ModelChildren
                .Where(mc => allChildLinks.All(cl =>
                    cl.AliasTo != mc.AliasTo && cl.To != mc.To)));
        }

        foreach (var childLink in allChildLinks)
        {
            var childAlias = !string.IsNullOrWhiteSpace(childLink.AliasTo)
                ? childLink.AliasTo
                : childLink.To;

            if (!trees.TryGetValue(childAlias, out var childTree))
            {
                var fallbackTree = trees.Values
                    .FirstOrDefault(t =>
                        t.Name.Equals(childAlias, StringComparison.OrdinalIgnoreCase) ||
                        t.Alias.Equals(childAlias, StringComparison.OrdinalIgnoreCase));

                if (fallbackTree == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(
                        $"[WARN] GenerateUpsertStatements: child alias '{childAlias}' " +
                        $"not found in trees. Skipping.");
                    Console.ResetColor();
                    continue;
                }

                childTree = fallbackTree;
            }

            GenerateUpsertStatements(
                trees, sqlUpsertStatementNodes, childTree,
                entityNames, sqlWhereStatement, entitiesProcessed,
                statements, selectStatements);
        }
    }
     
    /// <summary>
    /// Generate the main upsert INSERT ... ON CONFLICT statement.
    /// </summary>
    public static void GenerateUpsert(
        NodeTree currentTree,
        List<KeyValuePair<string, SqlNode>> currentColumns,
        string whereClause,
        List<string> statements)
    {
        if (currentColumns.Count == 0)
            return;
     
        // Only include columns that have a value set
        var columnsWithValues = currentColumns
            .Where(a => !string.IsNullOrEmpty(a.Value.Value))
            .ToList();
     
        if (columnsWithValues.Count == 0 || !columnsWithValues.Any(a => currentTree.UpsertKeys.Any(b => b.Split('~')[1].Matches(a.Value.Column))))
            return;
     
        var columnNames = string.Join(",",
            columnsWithValues.Select(s => $"\"{s.Value.Column}\""));
     
        var columnValues = string.Join(",",
            columnsWithValues.Select(s => $"'{s.Value.Value}'"));
     
        var sql =
            $" INSERT INTO \"{currentTree.Schema}\".\"{currentTree.Name}\" " +
            $"( {columnNames} ) VALUES ( {columnValues} ) " +
            $"ON CONFLICT ({string.Join(",", currentTree.UpsertKeys.Select(a => $"\"{a.Split('~')[1]}\""))}) ";
     
        // Exclude the conflict key column from the UPDATE SET clause
        var exclude = currentColumns
            // .Where(c => c.Value.UpsertKeys
            //     .Any(u => !u.Matches(c.Value.RelationshipKey.Split('~')[2])))
            .Select(e => $"\"{e.Value.Column}\" = EXCLUDED.\"{e.Value.Column}\"")
            .ToList();
     
        sql += exclude.Count > 0
            ? $" DO UPDATE SET {string.Join(",", exclude)} {whereClause};"
            : $" DO NOTHING {whereClause}";

        if (!string.IsNullOrEmpty(sql) && !statements.Contains(sql))
        {
            statements.Add(sql);
        }
    }
     
    /// <summary>
    /// Generate the relational upsert — INSERT ... SELECT from parent table.
    /// </summary>
    public static void GenerateSelectUpsert(
        NodeTree currentTree,
        List<string> entityNames,
        Dictionary<string, NodeTree> trees,
        List<KeyValuePair<string, SqlNode>> currentParentColumns,
        Dictionary<string, SqlNode> sqlUpsertStatementNodes,
        Dictionary<string, string> sqlWhereStatement,
        List<string> statements,
        List<string> selectStatements)
    {
        var currentColumns = sqlUpsertStatementNodes
            .Where(k =>
                k.Key.Split('~')[0].Matches(currentTree.Alias) && !k.Value.Column.Matches("Id"))
            .ToList();

        if (currentColumns.Count == 0)
            return;

        var modelToEntityLinks = currentTree.ModelToEntityLinks?.Count > 0
            ? currentTree.ModelToEntityLinks
            : currentTree.NodeMap?.ModelToEntityLinks ?? new List<LinkKey>();

        foreach (var modelToEntity in modelToEntityLinks)
        {
            GenerateUpsert(
                currentTree, currentColumns, sqlWhereStatement.GetValueOrDefault(currentTree.Alias) ?? string.Empty,
                statements);

            GenerateCommand(
                currentColumns, trees, modelToEntity.AliasFrom, currentParentColumns, sqlUpsertStatementNodes, selectStatements);
        }
    }
     
    private static void GenerateCommand(
        List<KeyValuePair<string, SqlNode>> currentColumns,
        Dictionary<string, NodeTree> trees,
        string entity,
        List<KeyValuePair<string, SqlNode>> currentParentColumns,
        Dictionary<string, SqlNode> sqlUpsertStatementNodes, 
        List<string> statements)
    {
        var sql = string.Empty;
        if (string.IsNullOrEmpty(entity))
            return;
        
        if (!trees.TryGetValue(entity, out var childTree))
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(
                $"[WARN] GenerateCommand: entity '{entity}' not found in trees. Skipping.");
            Console.ResetColor();
            return;
        }
        
        var linkKeyss = childTree.Parents
            .Join(
                trees,
                child => child.AliasTo,
                tree => tree.Key,
                (child, tree) => new
                {
                    LinkKey = child,
                    ToUpdate = !tree.Value.Mapping.Any(a => a.DestinationName.Matches(child.ToColumn)),
                    TreeId = tree.Value.Id
                }).OrderBy(k => k.TreeId).Select(k => k.LinkKey)
            .GroupBy(a => a.AliasTo)
            .Select(a => new { AliasTo = a.First().AliasTo, AliasFrom = a.First().AliasFrom,
                ToColumns = a.Select(v => v.ToColumn).ToList(), 
                FromColumns = a.Select(v => v.FromColumn).ToList() }).ToList();
        
        foreach (var linkKey in linkKeyss)
        {
            if (!trees.TryGetValue(linkKey.AliasTo, out var parentTree))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"[WARN] GenerateCommand: linkKey.AliasTo '{linkKey.AliasTo}' " +
                    $"not found in trees. Skipping.");
                Console.ResetColor();
                continue;
            }
     
            if (childTree.UpsertKeys.Count == 0 || parentTree.UpsertKeys.Count == 0)
                continue;
            
            var upsertKeysTo = trees[linkKey.AliasTo].UpsertKeys;
            
            var sqlNodeParentColumn = sqlUpsertStatementNodes
                .Where(a => upsertKeysTo.Any(b => b.Split('~')[1].Matches(a.Value.Column))).ToList();
     
            if (sqlNodeParentColumn == null)
                continue;
            
            var upsertKeysFrom = trees[linkKey.AliasFrom].UpsertKeys;
                
            var sqlNodeChildColumn = sqlUpsertStatementNodes
                .Where(a => upsertKeysFrom.Any(b => b.Split('~')[1].Matches(a.Value.Column))).ToList();
                
            if (sqlNodeChildColumn == null)
                continue;

            if (linkKey.ToColumns.All(b => parentTree.Mapping.Any(a => a.DestinationName.Matches(b)))) 
            {
                linkKey.ToColumns.AddRange(upsertKeysTo.Select(a => a.Split('~')[1]));
                linkKey.FromColumns.AddRange(upsertKeysTo.Select(a => a.Split('~')[1]));
                
                sql = GenerateStatement(childTree, linkKey.ToColumns, sqlNodeParentColumn, 
                    parentTree, linkKey.FromColumns,
                    sqlNodeChildColumn.Select(a => a.Value.Column).ToList(), true, sqlNodeChildColumn);
            }
            else
            {
                sql = GenerateStatement(parentTree, linkKey.FromColumns, sqlNodeParentColumn, childTree, linkKey.ToColumns, 
                    sqlNodeParentColumn.Select(a => a.Value.Column).ToList(), false, sqlNodeChildColumn);    
            }

            if (!string.IsNullOrEmpty(sql) && !statements.Contains(sql))
            {
                statements.Add(sql);
            }
        }
        
        var linkKeys = new List<LinkKey>();
        linkKeys.AddRange(childTree.RelatedParents
            .Join(
            trees,
            child => child.AliasTo,
            tree => tree.Key,
            (child, tree) => new
            {
                LinkKey = child,
                ToUpdate = !tree.Value.Mapping.Any(a => a.DestinationName.Matches(child.AliasTo)),
                TreeId = tree.Value.Id
            }).Where(a => a.ToUpdate).OrderBy(k => k.TreeId).Select(k => k.LinkKey));
        
        foreach (var linkKey in linkKeys)
        {
            if (!trees.TryGetValue(linkKey.AliasTo, out var parentTree))
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"[WARN] GenerateCommand: linkKey.AliasTo '{linkKey.AliasTo}' " +
                    $"not found in trees. Skipping.");
                Console.ResetColor();
                continue;
            }
     
            if (childTree.UpsertKeys.Count == 0 || parentTree.UpsertKeys.Count == 0)
                continue;
            
            var upsertKeys = trees[linkKey.AliasTo].UpsertKeys;
            
            var sqlNodeParentColumn = sqlUpsertStatementNodes
                .Where(a => upsertKeys.Any(b => b.Split('~')[1].Matches(a.Value.Column))).ToList();
     
            if (sqlNodeParentColumn == null || sqlNodeParentColumn.First().Value.FromComplexModel)
                continue;
            
            // var upsertKeysFrom = trees[linkKey.AliasFrom].UpsertKeys.Select(a => a.Split('~')[1]).ToList();
            //
            // var upsertKeysStatement = sqlNodeParentColumn.Where(a => upsertKeysFrom
            //     .Any(b => b.Matches(a.Value.Column))).Select(c => c.Value.Alias).ToList();
            
            var sqlNodeChildColumn = sqlUpsertStatementNodes
                .Where(a => upsertKeys.Any(b => b.Split('~')[1].Matches(a.Value.Column))).ToList();
     
            if (sqlNodeChildColumn == null)
                continue;
            
            sql = GenerateStatement(parentTree, new List<string>(){linkKey.ToColumn}, sqlNodeChildColumn, 
                childTree, new List<string>(){ linkKey.FromColumn },
                sqlNodeParentColumn.Select(a => a.Value.Column).ToList());
            
            if (!string.IsNullOrEmpty(sql) && !statements.Contains(sql))
            {
                statements.Add(sql);
            }
        }
    }

    private static string GenerateStatement(NodeTree parentTree,
        List<string> toColumns, List<KeyValuePair<string, SqlNode>> currentChildColumns, 
        NodeTree childTree, List<string> fromColumns, List<string> onConflict, bool isChild = false, List<KeyValuePair<string, SqlNode>> parentColumns = null)
    {
        var insertJoin = new List<string>();
        
        var selectJoin = new List<string>();

        var where = string.Empty;
        
        var excludeJoin = new List<string>();
        
        if (isChild)
        {
            // insertJoin.AddRange(fromColumns.Select(a => $"\"{a}\""));
            //
            // selectJoin.AddRange(toColumns.Select((a) => $"{childTree.Alias}.\"{a}\" AS \"{a}\""));
            //
            // excludeJoin.AddRange(fromColumns.Select(a => $"\"{a}\" = EXCLUDED.\"{a}\"").ToList());
            
            insertJoin.AddRange(fromColumns.Select(a => $"\"{a}\""));
            
            selectJoin.AddRange(fromColumns.Select((a, index) => $"{childTree.Alias}.\"{toColumns[index]}\" AS \"{a}\""));
            
            excludeJoin.AddRange(fromColumns.Select(a => $"\"{a}\" = EXCLUDED.\"{a}\"").ToList());
            
            insertJoin.AddRange(parentColumns.Select(a => $"\"{a.Value.Column}\""));
        
            selectJoin.AddRange(parentColumns.Select(a => $"'{a.Value.Value}' AS \"{a.Value.Column}\""));
            
            excludeJoin.AddRange(
                parentColumns.Select(a => $"\"{a.Value.Column}\" = EXCLUDED.\"{a.Value.Column}\""));
            
            where = string.Join(" AND ", currentChildColumns
                .Select(a => $"\"{a.Value.Column}\" = '{a.Value.Value}'"));
        }
        else
        {
            
            insertJoin.AddRange(fromColumns.Select(a => $"\"{a}\""));
            
            selectJoin.AddRange(fromColumns.Select((a, index) => $"{childTree.Alias}.\"{toColumns[index]}\" AS \"{a}\""));
            
            excludeJoin.AddRange(fromColumns.Select(a => $"\"{a}\" = EXCLUDED.\"{a}\"").ToList());
            
            insertJoin.AddRange(currentChildColumns.Select(a => $"\"{a.Value.Column}\""));
        
            selectJoin.AddRange(currentChildColumns.Select(a => $"'{a.Value.Value}' AS \"{a.Value.Column}\""));
            
            excludeJoin.AddRange(
                currentChildColumns.Select(a => $"\"{a.Value.Column}\" = EXCLUDED.\"{a.Value.Column}\""));
            
            where = string.Join(" AND ", parentColumns
                .Select(a => $"\"{a.Value.Column}\" = '{a.Value.Value}'"));
        }
        
        if (string.IsNullOrEmpty(where))
            return string.Empty;
     
        var sqlUpsertAux =
            $" INSERT INTO \"{parentTree.Schema}\".\"{parentTree.Name}\" " +
            $"( {string.Join(",", insertJoin)} ) " +
            $"( SELECT {string.Join(",", selectJoin)} " +
            $"FROM \"{childTree.Schema}\".\"{childTree.Name}\" {childTree.Alias} " +
            $"WHERE {where} ) " +
            $"ON CONFLICT ({string.Join(",", onConflict.Select((a, index) => $"\"{a}\"").ToList())}) ";
     
        sqlUpsertAux += excludeJoin.Count > 0
            ? $" DO UPDATE SET {string.Join(",", excludeJoin)}"
            : " DO NOTHING";
     
        return sqlUpsertAux;
    }
}