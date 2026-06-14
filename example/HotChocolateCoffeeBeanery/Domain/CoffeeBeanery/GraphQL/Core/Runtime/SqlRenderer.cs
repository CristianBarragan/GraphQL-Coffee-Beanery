using System.Text;
using CoffeeBeanery.GraphQL.Core.Contracts;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class SqlRenderer
{
    public static (string Sql, ProjectionMap ProjectionMap) Render(QueryPlan plan)
    {
        if (plan.Nodes.Count == 0)
            return (string.Empty, new ProjectionMap());

        var root = plan.Nodes[plan.RootAlias.Alias];
        var sql  = new StringBuilder();
        var map  = new ProjectionMap();

        // --- SELECT columns ---
        var selectColumns = new List<string>();
        int index = 0;

        foreach (var node in plan.Nodes.Values)
        {
            foreach (var column in node.Columns.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var colAlias = $"{node.Alias}_{column}";
                selectColumns.Add($"    {node.Alias}.\"{column}\" AS \"{colAlias}\"");
                map.Columns.Add(new SelectColumn
                {
                    Alias      = node.Alias,
                    Column     = column,
                    Property   = column,
                    Index      = index++,
                    TargetType = typeof(object)
                });
            }
        }

        sql.AppendLine("SELECT");
        sql.AppendLine(string.Join(",\n", selectColumns));

        // --- FROM with schema ---
        var schema = string.IsNullOrWhiteSpace(root.Schema) ? "public" : root.Schema;
        sql.AppendLine($"FROM \"{schema}\".\"{root.Table}\" {root.Alias}");

        // --- LEFT JOINs ---
        foreach (var join in JoinOrderResolver.Order(plan, root.Alias))
        {
            var target       = plan.Nodes[join.ToAlias];
            var targetSchema = string.IsNullOrWhiteSpace(target.Schema) ? "public" : target.Schema;

            sql.AppendLine(
                $"LEFT JOIN \"{targetSchema}\".\"{target.Table}\" {target.Alias} " +
                $"ON {join.FromAlias}.\"{join.FromColumn}\" = {join.ToAlias}.\"{join.ToColumn}\"");
        }

        // --- WHERE ---
        if (plan.Where?.Count > 0)
        {
            var clauses = plan.Where.Values.Where(v => !string.IsNullOrWhiteSpace(v)).ToList();
            if (clauses.Count > 0)
                sql.AppendLine("WHERE " + string.Join(" AND ", clauses));
        }

        // --- ORDER BY ---
        if (plan.OrderBy?.Count > 0)
        {
            var orderClauses = plan.OrderBy
                .Where(o => !string.IsNullOrWhiteSpace(o.Field))
                .Select(o =>
                {
                    // Resolve alias — find the node that owns this field
                    var ownerNode = plan.Nodes.Values
                        .FirstOrDefault(n => n.Columns.Any(c =>
                            c.Equals(o.Field, StringComparison.OrdinalIgnoreCase)));

                    var prefix = ownerNode != null
                        ? $"{ownerNode.Alias}."
                        : (o.Alias != null ? $"{o.Alias}." : string.Empty);

                    var dir = o.Direction == SortDirection.Desc ? "DESC" : "ASC";
                    return $"{prefix}\"{o.Field}\" {dir}";
                })
                .ToList();

            if (orderClauses.Count > 0)
                sql.AppendLine("ORDER BY " + string.Join(", ", orderClauses));
        }

        var innerSql = sql.ToString();

        // --- Pagination wrapper ---
        if (plan.HasPagination)
        {
            var p     = plan.Pagination;
            var start = p.First > 0 ? p.First : 1;
            var end   = p.Last  > 0 ? p.Last  : 20;

            // Matches the existing working pattern:
            // WITH publics AS (...) SELECT * FROM (SELECT COUNT(...) "RecordCount",
            // DENSE_RANK() OVER(ORDER BY root PK) AS "RowNumber", * FROM publics) a
            // WHERE "RowNumber" BETWEEN start AND end
            var rootPk = root.Columns.FirstOrDefault(c =>
                c.Equals("Id", StringComparison.OrdinalIgnoreCase)) ?? root.Columns.First();

            return (
                $" WITH publics AS (SELECT * FROM ({innerSql}) {root.Alias} )" +
                $" SELECT * FROM (" +
                $" SELECT (SELECT DISTINCT COUNT(1) FROM publics) \"RecordCount\"," +
                $"  DENSE_RANK() OVER( ORDER BY \"{rootPk}_\") AS \"RowNumber\", * FROM publics) a" +
                $"  WHERE \"RowNumber\" BETWEEN {start} AND {end}",
                map);
        }

        return (innerSql, map);
    }
}