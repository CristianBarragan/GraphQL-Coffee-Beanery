// SqlQueryCompiler.cs — unchanged signature, returns SplitOnDapper on context
using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Language;

internal static class SqlQueryCompiler
{
    public static SqlCompileResult Compile(
        SqlCompilationContext context,
        FieldNode             rootSelection,
        GraphIL               graph,
        string                rootAlias,
        IQueryPlanner         queryPlanner)
    {
        var orderInstructions = new List<OrderInstruction>();

        foreach (var arg in rootSelection.Arguments)
        {
            switch (arg.Name.ToString())
            {
                case "first":
                    context.Pagination.First =
                        int.TryParse(arg.Value?.Value?.ToString(), out var f) ? f : 0;
                    context.HasPagination = true;
                    break;
                case "last":
                    context.Pagination.Last =
                        int.TryParse(arg.Value?.Value?.ToString(), out var l) ? l : 0;
                    context.HasPagination = true;
                    break;
                case "before":
                    context.Pagination.Before = arg.Value?.Value?.ToString();
                    context.HasPagination = true;
                    break;
                case "after":
                    context.Pagination.After = arg.Value?.Value?.ToString();
                    context.HasPagination = true;
                    break;
                case "order":
                    orderInstructions.AddRange(SqlOrderCompiler.Compile(arg));
                    break;
                default:
                    if (arg.Name.ToString().Contains("order"))
                        orderInstructions.AddRange(SqlOrderCompiler.Compile(arg));
                    break;
            }
        }

        if (context.HasPagination)
            SqlPagingCompiler.ExtractPagination(context, rootSelection);
        
        var selectedAliases = ExtractSelectedAliases(rootSelection, graph);

        // Build plan first — everything else depends on it
        var plan = queryPlanner.Build(graph, rootAlias)
            ?? throw new InvalidOperationException("QueryPlanner produced no plan");

        // Parse WHERE into plan now that plan exists
        var whereArg = rootSelection.Arguments
            .FirstOrDefault(a => a.Name.ToString()
                .Equals("where", StringComparison.OrdinalIgnoreCase));

        if (whereArg != null)
            ParseWhereIntoPlan(whereArg, graph, plan);

        // Push order/pagination onto plan so SqlRenderer can use them
        plan.OrderBy       = orderInstructions;
        plan.Pagination    = context.Pagination;
        plan.HasPagination = context.HasPagination;

        Console.WriteLine($"=== SqlQueryCompiler: plan built ===");
        Console.WriteLine($"  nodes      = {string.Join(", ", plan.Nodes.Keys)}");
        Console.WriteLine($"  joins      = {plan.Joins.Count}");
        Console.WriteLine($"  where      = {string.Join("; ", plan.Where.Select(kv => $"{kv.Key}:{kv.Value}"))}");
        Console.WriteLine($"  order      = {string.Join(", ", plan.OrderBy.Select(o => $"{o.Field} {o.Direction}"))}");
        Console.WriteLine($"  pagination = first={plan.Pagination.First} last={plan.Pagination.Last}");

        SqlSelectBuilder.HandleGraphQL(context, plan);

        return new SqlCompileResult(context.SelectSql, context.Projection, context.SplitOnDapper)
        {
            SplitOnDapper = context.SplitOnDapper
        };
    }
    
    private static HashSet<string> ExtractSelectedAliases(
        FieldNode rootSelection,
        GraphIL   graph)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        WalkSelection(rootSelection, graph, result);
        return result;
    }

    private static void WalkSelection(
        ISyntaxNode         node,
        GraphIL             graph,
        HashSet<string>     result)
    {
        foreach (var child in node.GetNodes())
        {
            var text = child.ToString().Trim();

            // Match node name against graph aliases — camelCase → PascalCase
            var pascalName = text.Length > 0
                ? char.ToUpper(text[0]) + text[1..]
                : text;

            // Direct alias match
            foreach (var alias in graph.Nodes.Keys)
            {
                if (alias.EndsWith(pascalName, StringComparison.OrdinalIgnoreCase) ||
                    alias.Equals(pascalName, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(alias);
                }
            }

            WalkSelection(child, graph, result);
        }
    }
    
    private static void ParseWhereIntoPlan(
        ArgumentNode whereArg,
        GraphIL      graph,
        QueryPlan    plan)
    {
        // Walk the where argument tree, match field names to graph node columns,
        // emit SQL conditions keyed by node alias.
        WalkWhere(whereArg, graph, plan, currentAlias: null);
    }

    private static void WalkWhere(
        ISyntaxNode node,
        GraphIL     graph,
        QueryPlan   plan,
        string?     currentAlias)
    {
        foreach (var child in node.GetNodes())
        {
            var text = child.ToString();

            // Skip collection operators — just recurse into them
            if (text.StartsWith("some:") || text.StartsWith("all:") ||
                text.StartsWith("none:") || text.StartsWith("any:"))
            {
                WalkWhere(child, graph, plan, currentAlias);
                continue;
            }

            var colonIdx = text.IndexOf(':');
            if (colonIdx <= 0)
            {
                WalkWhere(child, graph, plan, currentAlias);
                continue;
            }

            var fieldName = text[..colonIdx].Trim();
            var rest      = text[(colonIdx + 1)..].Trim();

            // Is this a nested object navigation (e.g. "innerCustomer: { ... }")?
            if (rest.StartsWith("{"))
            {
                // Resolve which node alias this field navigates to
                var resolvedAlias = ResolveAliasForField(fieldName, currentAlias, graph)
                                    ?? currentAlias;
                WalkWhere(child, graph, plan, resolvedAlias);
                continue;
            }

            // Is this a leaf operator (e.g. "in: \"2dc3...\"" )?
            var operatorIdx = rest.IndexOf(':');
            if (operatorIdx > 0)
            {
                var op    = rest[..operatorIdx].Trim();
                var value = rest[(operatorIdx + 1)..].Trim().Trim('"');

                if (currentAlias == null) continue;

                // Find the column in the target node
                if (!plan.Nodes.TryGetValue(currentAlias, out var planNode)) continue;

                var column = planNode.Columns.FirstOrDefault(c =>
                    c.Equals(fieldName, StringComparison.OrdinalIgnoreCase));

                // Try matching by camelCase → PascalCase
                if (column == null)
                    column = planNode.Columns.FirstOrDefault(c =>
                        c.Equals(
                            char.ToUpper(fieldName[0]) + fieldName[1..],
                            StringComparison.OrdinalIgnoreCase));

                if (column == null) continue;

                var condition = op switch
                {
                    "eq"  => $"{currentAlias}.\"{column}\" = '{EscapeSql(value)}'",
                    "neq" => $"{currentAlias}.\"{column}\" <> '{EscapeSql(value)}'",
                    "in"  => $"{currentAlias}.\"{column}\" IN ({BuildInList(value)})",
                    _     => string.Empty
                };

                if (string.IsNullOrEmpty(condition)) continue;

                if (plan.Where.TryGetValue(currentAlias, out var existing))
                    plan.Where[currentAlias] = existing + " AND " + condition;
                else
                    plan.Where[currentAlias] = condition;
            }

            WalkWhere(child, graph, plan, currentAlias);
        }
    }

    private static string? ResolveAliasForField(
        string   fieldName,
        string?  currentAlias,
        GraphIL  graph)
    {
        // Look for a node alias that starts with or matches the field name
        return graph.Nodes.Keys.FirstOrDefault(alias =>
            alias.EndsWith(
                char.ToUpper(fieldName[0]) + fieldName[1..],
                StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildInList(string value)
    {
        var parts = value.Trim('[', ']').Split(',');
        return string.Join(", ", parts.Select(p => $"'{EscapeSql(p.Trim())}'"));
    }

    private static string EscapeSql(string value)
        => value.Replace("'", "''");
}