using System.Text;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class SqlHelper
{
    public static void GenerateUpsertStatements(
        Dictionary<string, NodeTree> modelTrees,
        Dictionary<string, NodeTree> entityTrees,
        Dictionary<string, SqlNode> sqlUpsertStatementNodes,
        NodeTree rootTree,
        List<string> entityNames,
        Dictionary<string, string> sqlWhereStatement,
        List<string> entitiesProcessed,
        List<string> statements,
        List<string> selectStatements)
    {
        var aliasesToProcess = sqlUpsertStatementNodes
            .Select(k => k.Key.Split('~')[0])
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<NodeTree>();

        foreach (var link in rootTree.ModelToEntityLinks)
        {
            if (entityTrees.TryGetValue(link.AliasTo, out var seed) &&
                !visited.Contains(seed.Alias))
                queue.Enqueue(seed);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!visited.Add(current.Alias))
                continue;

            if (aliasesToProcess.Contains(current.Alias) &&
                !entitiesProcessed.Contains(current.Alias))
            {
                entitiesProcessed.Add(current.Alias);

                var currentColumns = sqlUpsertStatementNodes
                    .Where(k =>
                        k.Key.Split('~')[0].Matches(current.Alias) &&
                        entityNames.Contains(k.Value.RelationshipKey.Split('~')[1]))
                    .ToList();

                if (currentColumns.Count > 0)
                {
                    GenerateSelectUpsert(
                        current, entityNames, entityTrees, currentColumns,
                        sqlUpsertStatementNodes, sqlWhereStatement,
                        statements, selectStatements);
                }
            }

            foreach (var childLink in current.Children.Concat(current.RelatedChildren))
            {
                var childAlias = !string.IsNullOrWhiteSpace(childLink.AliasTo)
                    ? childLink.AliasTo
                    : childLink.To;

                if (entityTrees.TryGetValue(childAlias, out var childTree) &&
                    !visited.Contains(childTree.Alias))
                {
                    queue.Enqueue(childTree);
                }
            }
        }
    }

    public static void GenerateUpsert(
        NodeTree currentTree,
        List<KeyValuePair<string, SqlNode>> currentColumns,
        string whereClause,
        List<string> statements)
    {
        if (currentColumns.Count == 0)
            return;

        var columnsWithValues = currentColumns
            .Where(a => !string.IsNullOrEmpty(a.Value.Value))
            .ToList();

        if (columnsWithValues.Count == 0 ||
            !columnsWithValues.Any(a => currentTree.UpsertKeys.Any(b => b.Split('~')[1].Matches(a.Value.Column))))
            return;

        var columnNames = string.Join(",",
            columnsWithValues.Select(s => $"\"{s.Value.Column}\"").Distinct());

        var columnValues = string.Join(",",
            columnsWithValues.Select(s => $"'{s.Value.Value}'").Distinct());

        var sql =
            $" INSERT INTO \"{currentTree.Schema}\".\"{currentTree.Name}\" " +
            $"( {columnNames} ) VALUES ( {columnValues} ) " +
            $"ON CONFLICT ({string.Join(",", currentTree.UpsertKeys.Select(a => $"\"{a.Split('~')[1]}\""))}) ";

        var exclude = currentColumns
            .Select(e => $"\"{e.Value.Column}\" = EXCLUDED.\"{e.Value.Column}\"")
            .Distinct().ToList();

        sql += exclude.Count > 0
            ? $" DO UPDATE SET {string.Join(",", exclude)} {whereClause};"
            : $" DO NOTHING {whereClause}";

        if (!string.IsNullOrEmpty(sql) && !statements.Contains(sql))
        {
            statements.Add(sql);
        }
    }

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

        foreach (var modelToEntity in currentTree.ModelToEntityLinks)
        {
            GenerateUpsert(
                currentTree, currentColumns, sqlWhereStatement.GetValueOrDefault(currentTree.Alias) ?? string.Empty,
                statements);

            GenerateCommand(
                trees, modelToEntity.AliasFrom, sqlUpsertStatementNodes,
                selectStatements);

            if (currentTree.GraphMap != null)
            {
                var map = currentTree.GraphMap;
                var sql = BuildMergeRelationship(
                    graphName: map.GraphName,

                    fromLabel: map.FromVertex.Label,
                    fromKeyColumn: map.FromVertex.KeyColumn,
                    fromValue: sqlUpsertStatementNodes
                        .FirstOrDefault(a => a.Value.Column.Matches(map.FromVertex.KeyColumn)).Value.Value,

                    toLabel: map.ToVertex.Label,
                    toKeyColumn: map.ToVertex.KeyColumn,
                    toValue: sqlUpsertStatementNodes.FirstOrDefault(a => a.Value.Column.Matches(map.ToVertex.KeyColumn))
                        .Value.Value,

                    edgeLabel: map.EdgeLabel,
                    edgeKeyColumn: map.EdgeKeyColumn,
                    edgeValue: sqlUpsertStatementNodes.FirstOrDefault(a => a.Value.Column.Matches(map.EdgeKeyColumn))
                        .Value.Value,
                    edgeProperties: new Dictionary<string, string>
                    {
                        {
                            map.EdgeKeyColumn,
                            sqlUpsertStatementNodes.FirstOrDefault(a => a.Value.Column.Matches(map.EdgeKeyColumn)).Value
                                .Value
                        }
                    }
                );

                if (!selectStatements.Contains(sql))
                {
                    selectStatements.Add(sql);
                }
            }
        }
    }

    public static string BuildMergeRelationship(
        string graphName,
        string fromLabel,
        string fromKeyColumn,
        string fromValue,
        string toLabel,
        string toKeyColumn,
        string toValue,
        string edgeLabel,
        string edgeKeyColumn,
        string edgeValue,
        Dictionary<string, string>? edgeProperties = null)
    {
        var setClause = BuildSetClause(edgeProperties);

        return @$"
            ;CREATE TEMP TABLE temp_merge AS SELECT 1 
            FROM ag_catalog.cypher(
                '{graphName}',
                $$

                MERGE (a:{fromLabel} {{ {fromKeyColumn}: '{EscapeValue(fromValue)}' }})
                MERGE (b:{toLabel} {{ {toKeyColumn}: '{EscapeValue(toValue)}' }})

                MERGE (a)-[r:{edgeLabel}]->(b)

                {setClause}

                RETURN r.{edgeLabel}::text

                $$
            ) AS (r text); DROP TABLE temp_merge;
            ";
    }

    private static string BuildSetClause(Dictionary<string, string>? props)
    {
        if (props == null || props.Count == 0)
            return string.Empty;

        return "SET " + string.Join(", ",
            props.Select(p => $"r.{p.Key} = '{EscapeValue(p.Value)}'"));
    }

    private static void GenerateCommand(
        Dictionary<string, NodeTree> trees,
        string entity,
        Dictionary<string, SqlNode> sqlUpsertStatementNodes,
        List<string> statements)
    {
        if (string.IsNullOrEmpty(entity))
            return;

        if (!trees.TryGetValue(entity, out var currentTree))
        {
            return;
        }

        if (currentTree.UpsertKeys.Count == 0)
            return;

        foreach (var linkKey in currentTree.Children.Concat(currentTree.RelatedChildren))
        {
            // LinkKey tells us exactly:
            //   AliasFrom = the table being updated (CustomerCustomerRelationship)
            //   FromColumn = the FK column to write into (InnerCustomerId / OuterCustomerId)
            //   AliasTo = the parent table alias to SELECT from (InnerCustomer / OuterCustomer)
            //   ToColumn = the PK column to read (Id)

            if (!trees.TryGetValue(linkKey.AliasTo, out var parentTree))
            {
                continue;
            }

            // The child table is the current entity (CustomerCustomerRelationship)
            // Its upsert key drives ON CONFLICT and WHERE
            var childUpsertKeys = currentTree.UpsertKeys;

            var childKeyNodes = sqlUpsertStatementNodes
                .Where(a => childUpsertKeys.Any(k =>
                    a.Key.Matches($"{linkKey.AliasFrom}~{k.Split('~')[0]}~{k.Split('~')[1]}")))
                .Where(a => !string.IsNullOrEmpty(a.Value.Value))
                .DistinctBy(a => a.Value.Column)
                .ToList();

            if (!childKeyNodes.Any())
            {
                continue;
            }

            // Parent upsert keys drive the WHERE on the SELECT
            // e.g. WHERE "CustomerKey" = '0dc3...'
            var parentUpsertKeys = parentTree.UpsertKeys;

            var parentWhereNodes = sqlUpsertStatementNodes
                .Where(a => parentUpsertKeys.Any(k =>
                    a.Key.Matches($"{linkKey.AliasTo}~{k.Split('~')[0]}~{k.Split('~')[1]}")))
                .Where(a => !string.IsNullOrEmpty(a.Value.Value))
                .DistinctBy(a => a.Value.Column)
                .ToList();

            if (!parentWhereNodes.Any())
            {
                continue;
            }

            var sql = GenerateRelationshipStatement(
                childTree:        currentTree,
                fkColumn:         linkKey.FromColumn,    // InnerCustomerId / OuterCustomerId
                pkColumn:         linkKey.ToColumn,      // Id
                parentTree:       parentTree,
                parentWhereNodes: parentWhereNodes,      // WHERE CustomerKey = '0dc3...'
                childKeyNodes:    childKeyNodes,         // ON CONFLICT CustomerCustomerRelationshipKey
                onConflictCols:   childUpsertKeys
                                    .Select(k => k.Split('~')[1])
                                    .Distinct()
                                    .ToList());

            if (!string.IsNullOrEmpty(sql) && !statements.Contains(sql))
                statements.Add(sql);
        }
    }

    private static string GenerateRelationshipStatement(
        NodeTree childTree,
        string fkColumn,                                         // InnerCustomerId
        string pkColumn,                                         // Id
        NodeTree parentTree,
        List<KeyValuePair<string, SqlNode>> parentWhereNodes,    // WHERE CustomerKey = '0dc3...'
        List<KeyValuePair<string, SqlNode>> childKeyNodes,       // CustomerCustomerRelationshipKey value
        List<string> onConflictCols)                             // ["CustomerCustomerRelationshipKey"]
    {
        if (string.IsNullOrEmpty(fkColumn) || string.IsNullOrEmpty(pkColumn))
        {
            return string.Empty;
        }

        // INSERT ( "InnerCustomerId", "CustomerCustomerRelationshipKey" )
        var insertCols = new List<string> { $"\"{fkColumn}\"" };
        insertCols.AddRange(childKeyNodes.Select(a => $"\"{a.Value.Column}\""));

        // SELECT parent."Id" AS "InnerCustomerId", '3dc3...' AS "CustomerCustomerRelationshipKey"
        var selectExprs = new List<string>
        {
            $"{parentTree.Alias}.\"{pkColumn}\" AS \"{fkColumn}\""
        };
        selectExprs.AddRange(
            childKeyNodes.Select(a => $"'{EscapeValue(a.Value.Value)}' AS \"{a.Value.Column}\""));

        // WHERE "CustomerKey" = '0dc3...'
        var parentWhere = string.Join(" AND ",
            parentWhereNodes.Select(a =>
                $"\"{a.Value.Column}\" = '{EscapeValue(a.Value.Value)}'"));

        // DO UPDATE SET "InnerCustomerId" = EXCLUDED."InnerCustomerId"
        // Never include the conflict target column in SET
        var setClauses = new List<string>
        {
            $"\"{fkColumn}\" = EXCLUDED.\"{fkColumn}\""
        };

        var onConflictExpr = string.Join(", ", onConflictCols.Select(c => $"\"{c}\""));

        return
            $"INSERT INTO \"{childTree.Schema}\".\"{childTree.Name}\" " +
            $"( {string.Join(", ", insertCols)} ) " +
            $"( SELECT {string.Join(", ", selectExprs)} " +
            $"FROM \"{parentTree.Schema}\".\"{parentTree.Name}\" {parentTree.Alias} " +
            $"WHERE {parentWhere} ) " +
            $"ON CONFLICT ({onConflictExpr}) " +
            $"DO UPDATE SET {string.Join(", ", setClauses)}";
    }

    private static string EscapeValue(string value)
        => value?.Replace("'", "''") ?? string.Empty;
}

