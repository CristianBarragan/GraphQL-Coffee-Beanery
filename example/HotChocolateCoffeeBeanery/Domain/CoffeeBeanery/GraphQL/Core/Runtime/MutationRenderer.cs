// MutationRenderer.cs — full rewrite to support FK-CTE chaining
using System.Text;
using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.GraphQL.Core.Sql;

public static class MutationRenderer
{
    // MutationRenderer.cs
public static (List<string> Inserts, List<string> GraphMerges) Render(
    MutationPlan plan,
    GraphIL      graph)
{
    var inserts     = new List<string>();
    var graphMerges = new List<string>();

    // Topologically sort: nodes whose aliases appear as FK sources come first
    var ordered = TopologicalSort(plan, graph);

    // Find nodes that are FK parents of other nodes in the plan
    var fkDeps      = ResolveFkDependencies(plan, graph);
    var fkParents   = fkDeps.Values
        .SelectMany(d => d.Select(f => f.ParentAlias))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var fkChildren  = fkDeps.Keys
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    // Group: nodes that are neither parent nor child → standalone
    foreach (var node in ordered)
    {
        if (fkChildren.Contains(node.Alias))
            continue; // handled below as FK batch
        if (fkParents.Contains(node.Alias))
            continue; // handled inside FK batch

        if (!graph.Nodes.TryGetValue(node.Alias, out var graphNode))
            continue;

        var sql = BuildStandaloneUpsert(node, graphNode);
        if (!string.IsNullOrWhiteSpace(sql))
            inserts.Add(sql);
    }

    // Emit FK batches: WITH cte_parent AS (...) INSERT...SELECT
    foreach (var (childAlias, deps) in fkDeps)
    {
        if (!plan.Nodes.TryGetValue(childAlias, out var childNode)) continue;
        if (!graph.Nodes.TryGetValue(childAlias, out var childGraphNode)) continue;

        var batch = BuildFkCteBatch(childNode, childGraphNode, deps, plan, graph);
        if (!string.IsNullOrWhiteSpace(batch))
            inserts.Add(batch);
    }

    // Graph MERGE statements
    foreach (var node in plan.Nodes.Values)
    {
        if (!graph.Nodes.TryGetValue(node.Alias, out var graphNode)) continue;
        if (graphNode.GraphMap == null) continue;

        var merge = BuildGraphMerge(graphNode.GraphMap, plan);
        if (!string.IsNullOrWhiteSpace(merge))
            graphMerges.Add(merge);
    }

    return (inserts, graphMerges);
}

private static List<MutationNode> TopologicalSort(MutationPlan plan, GraphIL graph)
{
    var result  = new List<MutationNode>();
    var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    void Visit(string alias)
    {
        if (!visited.Add(alias)) return;
        if (!plan.Nodes.TryGetValue(alias, out var node)) return;

        // Visit parents first (nodes this one has inbound edges from)
        if (graph.EdgesByTargetAlias.TryGetValue(alias, out var inbound))
        {
            foreach (var edge in inbound)
            {
                if (plan.Nodes.ContainsKey(edge.FromAlias))
                    Visit(edge.FromAlias);
            }
        }

        result.Add(node);
    }

    foreach (var alias in plan.Nodes.Keys)
        Visit(alias);

    return result;
}

private sealed record FkDep(
    string       ParentAlias,
    string       CteName,
    string       FkColumn,
    string       PkColumn,
    MutationNode ParentNode,
    GraphILNode  ParentGraphNode);

private static Dictionary<string, List<FkDep>> ResolveFkDependencies(
    MutationPlan plan,
    GraphIL      graph)
{
    var result = new Dictionary<string, List<FkDep>>(StringComparer.OrdinalIgnoreCase);

    foreach (var (childAlias, childNode) in plan.Nodes)
    {
        if (!graph.Nodes.TryGetValue(childAlias, out var childGraphNode)) continue;
        if (!graph.EdgesByTargetAlias.TryGetValue(childAlias, out var inbound)) continue;

        foreach (var edge in inbound)
        {
            if (!plan.Nodes.TryGetValue(edge.FromAlias, out var parentNode)) continue;
            if (!graph.Nodes.TryGetValue(edge.FromAlias, out var parentGraphNode)) continue;

            // Look for a column on the child that looks like ParentAlias + "Id"
            // e.g. edge.FromAlias="InnerCustomerCustomer" → fkColumn="InnerCustomerId"
            var fkColumn = childNode.IdentityFields
                .Concat(childNode.DataFields)
                .Select(f => f.Column)
                .FirstOrDefault(col =>
                    col.EndsWith("Id", StringComparison.OrdinalIgnoreCase) &&
                    edge.FromAlias.StartsWith(
                        col[..^2], // strip "Id"
                        StringComparison.OrdinalIgnoreCase));

            if (fkColumn == null) continue;

            if (!result.TryGetValue(childAlias, out var deps))
                result[childAlias] = deps = new List<FkDep>();

            deps.Add(new FkDep(
                ParentAlias:     edge.FromAlias,
                CteName:         $"cte_{edge.FromAlias}",
                FkColumn:        fkColumn,
                PkColumn:        "Id",
                ParentNode:      parentNode,
                ParentGraphNode: parentGraphNode));
        }
    }

    return result;
}

private static string BuildFkCteBatch(
    MutationNode  childNode,
    GraphILNode   childGraphNode,
    List<FkDep>   deps,
    MutationPlan  plan,
    GraphIL       graph)
{
    var sb       = new StringBuilder();
    var cteTerms = new List<string>();

    foreach (var dep in deps)
    {
        var parentFields = dep.ParentNode.IdentityFields
            .Concat(dep.ParentNode.DataFields)
            .DistinctBy(f => f.Column)
            .ToList();

        if (parentFields.Count == 0) continue;

        var cols     = string.Join(", ", parentFields.Select(f => $"\"{f.Column}\""));
        var vals     = string.Join(", ", parentFields.Select(f => FormatValue(f.Value)));
        var conflict = string.Join(", ",
            dep.ParentNode.IdentityFields.Select(f => $"\"{f.Column}\"").Distinct());
        var setExprs = string.Join(", ",
            dep.ParentNode.DataFields
                .Select(f => $"\"{f.Column}\" = EXCLUDED.\"{f.Column}\"")
                .Distinct());

        var schema = string.IsNullOrWhiteSpace(dep.ParentGraphNode.Schema)
            ? "public" : dep.ParentGraphNode.Schema;

        cteTerms.Add(
            $"    {dep.CteName} AS (\n" +
            $"        INSERT INTO \"{schema}\".\"{dep.ParentGraphNode.TableName}\"" +
            $" ( {cols} )\n" +
            $"        VALUES ( {vals} )\n" +
            $"        ON CONFLICT ({conflict}) DO UPDATE SET {setExprs}\n" +
            $"        RETURNING \"Id\"\n" +
            $"    )");
    }

    if (cteTerms.Count == 0)
        return BuildStandaloneUpsert(childNode, childGraphNode);

    sb.AppendLine("WITH");
    sb.Append(string.Join(",\n", cteTerms));
    sb.AppendLine();

    var fkCols     = deps.Select(d => d.FkColumn)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);
    var scalarFields = childNode.IdentityFields
        .Concat(childNode.DataFields)
        .Where(f => !fkCols.Contains(f.Column))
        .DistinctBy(f => f.Column)
        .ToList();

    var insertCols  = new List<string>();
    var selectExprs = new List<string>();
    var fromCtes    = new List<string>();
    var setClauses  = new List<string>();

    var conflictSet = childNode.IdentityFields
        .Select(f => f.Column)
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    foreach (var f in scalarFields)
    {
        insertCols.Add($"\"{f.Column}\"");
        selectExprs.Add($"{FormatValue(f.Value)} AS \"{f.Column}\"");
        if (!conflictSet.Contains(f.Column))
            setClauses.Add($"\"{f.Column}\" = EXCLUDED.\"{f.Column}\"");
    }

    foreach (var dep in deps)
    {
        insertCols.Add($"\"{dep.FkColumn}\"");
        selectExprs.Add($"{dep.CteName}.\"{dep.PkColumn}\" AS \"{dep.FkColumn}\"");
        fromCtes.Add(dep.CteName);
        setClauses.Add($"\"{dep.FkColumn}\" = EXCLUDED.\"{dep.FkColumn}\"");
    }

    var conflictOn = string.Join(", ",
        childNode.IdentityFields.Select(f => $"\"{f.Column}\""));
    var childSchema = string.IsNullOrWhiteSpace(childGraphNode.Schema)
        ? "public" : childGraphNode.Schema;

    sb.AppendLine($"INSERT INTO \"{childSchema}\".\"{childGraphNode.TableName}\"");
    sb.AppendLine($"    ( {string.Join(", ", insertCols)} )");
    sb.AppendLine($"SELECT {string.Join(", ", selectExprs)}");
    sb.AppendLine($"FROM   {string.Join(", ", fromCtes)}");
    sb.AppendLine($"ON CONFLICT ({conflictOn})");
    sb.Append(setClauses.Count > 0
        ? $"DO UPDATE SET {string.Join(", ", setClauses)};"
        : "DO NOTHING;");

    return sb.ToString();
}

private static string BuildStandaloneUpsert(MutationNode node, GraphILNode graphNode)
{
    var allFields = node.IdentityFields.Concat(node.DataFields).ToList();
    if (allFields.Count == 0) return string.Empty;

    var columns  = string.Join(", ", allFields.Select(f => $"\"{f.Column}\""));
    var values   = string.Join(", ", allFields.Select(f => FormatValue(f.Value)));
    var conflict = string.Join(", ",
        node.IdentityFields.Select(f => $"\"{f.Column}\"").Distinct());
    var setClause = node.DataFields.Count > 0
        ? "DO UPDATE SET " + string.Join(", ",
            node.DataFields.Select(f =>
                $"\"{f.Column}\" = EXCLUDED.\"{f.Column}\""))
        : "DO NOTHING";

    var schema = string.IsNullOrWhiteSpace(graphNode.Schema)
        ? "public" : graphNode.Schema;

    return
        $"INSERT INTO \"{schema}\".\"{graphNode.TableName}\"\n" +
        $"({columns})\n" +
        $"VALUES ({values})\n" +
        $"ON CONFLICT ({conflict})\n" +
        $"{setClause}";
}

private static string BuildGraphMerge(GraphMap map, MutationPlan plan)
{
    // GraphVertex.KeyColumn is the graph property name (e.g. "InnerCustomerKey")
    // but plan nodes store values under the entity's own column name (e.g. "CustomerKey").
    // Use AliasTo to find the right node, then take its first identity field value.
    var fromValue = GetFieldValueByAlias(plan, map.FromVertex.AliasTo, map.FromVertex.KeyColumn)
                 ?? GetFieldValue(plan, map.FromVertex.KeyColumn);

    var toValue   = GetFieldValueByAlias(plan, map.ToVertex.AliasTo, map.ToVertex.KeyColumn)
                 ?? GetFieldValue(plan, map.ToVertex.KeyColumn);

    var edgeValue = GetFieldValue(plan, map.EdgeKeyColumn);

    Console.WriteLine($"=== BuildGraphMerge ===");
    Console.WriteLine($"  FromVertex.AliasTo={map.FromVertex.AliasTo} KeyColumn={map.FromVertex.KeyColumn} → '{fromValue}'");
    Console.WriteLine($"  ToVertex.AliasTo={map.ToVertex.AliasTo}   KeyColumn={map.ToVertex.KeyColumn} → '{toValue}'");
    Console.WriteLine($"  EdgeKeyColumn={map.EdgeKeyColumn} → '{edgeValue}'");

    if (string.IsNullOrEmpty(fromValue) || string.IsNullOrEmpty(toValue))
        return string.Empty;

    var setClause = string.IsNullOrEmpty(edgeValue) ? string.Empty
        : $"SET r.{map.EdgeKeyColumn} = '{Escape(edgeValue)}'";

    return
        $";CREATE TEMP TABLE temp_merge AS SELECT 1\n" +
        $"FROM ag_catalog.cypher(\n" +
        $"    '{map.GraphName}',\n" +
        $"    $$\n" +
        $"    MERGE (a:{map.FromVertex.Label} {{ {map.FromVertex.KeyColumn}: '{Escape(fromValue)}' }})\n" +
        $"    MERGE (b:{map.ToVertex.Label} {{ {map.ToVertex.KeyColumn}: '{Escape(toValue)}' }})\n" +
        $"    MERGE (a)-[r:{map.EdgeLabel}]->(b)\n" +
        $"    {setClause}\n" +
        $"    RETURN r.{map.EdgeLabel}::text\n" +
        $"    $$\n" +
        $") AS (r text); DROP TABLE temp_merge;";
}

// Look up by node alias first, try exact KeyColumn match, then fall back to first identity field
private static string? GetFieldValueByAlias(MutationPlan plan, string aliasTo, string keyColumn)
{
    if (string.IsNullOrEmpty(aliasTo)) return null;
    if (!plan.Nodes.TryGetValue(aliasTo, out var node)) return null;

    // Try exact column name match first
    var exact = node.IdentityFields.Concat(node.DataFields)
        .FirstOrDefault(f => string.Equals(f.Column, keyColumn, StringComparison.OrdinalIgnoreCase));
    if (exact != null) return exact.Value?.ToString();

    // Fall back to first identity field — the natural key of this entity
    return node.IdentityFields.FirstOrDefault()?.Value?.ToString();
}

private static string? GetFieldValue(MutationPlan plan, string columnName)
{
    foreach (var node in plan.Nodes.Values)
    {
        var field = node.IdentityFields.Concat(node.DataFields)
            .FirstOrDefault(f => string.Equals(
                f.Column, columnName, StringComparison.OrdinalIgnoreCase));
        if (field != null) return field.Value?.ToString();
    }
    return null;
}

private static string FormatValue(object? value) => value switch
{
    null     => "NULL",
    string s => $"'{s.Replace("'", "''")}'",
    bool b   => b ? "TRUE" : "FALSE",
    _        => value.ToString()!
};

private static string Escape(string value)
    => value?.Replace("'", "''") ?? string.Empty;
}