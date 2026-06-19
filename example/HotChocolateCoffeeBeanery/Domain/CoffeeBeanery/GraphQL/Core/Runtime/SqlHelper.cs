using System.Text;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

/// <summary>
/// Builds upsert/CTE SQL directly from a MutationGraphWalker.Result - i.e. straight from
/// (alias -> List&lt;(Column, Value)&gt;), with no RelationshipKey string-splitting and no
/// re-filtering of a flat node dictionary per alias per call. The previous version called
/// `sqlUpsertStatementNodes.Where(k => k.Key.Split('~')[0] == alias)` for every alias, every
/// FK link, every recursion step - here that's a single dictionary lookup since the walker
/// already grouped everything by alias up front.
/// </summary>
public static class SqlHelper
{
    public static void GenerateUpsertStatements(
        Dictionary<string, EntityNodeTree> entityTrees,
        MutationGraphWalker.Result mutationData,
        EntityNodeTree rootTree,
        Dictionary<string, string> sqlWhereStatement,
        List<string> statements,
        List<string> selectStatements)
    {
        var columnsByAlias = mutationData.ColumnsByAlias;
        var aliasesToProcess = new HashSet<string>(mutationData.AliasOrder, StringComparer.OrdinalIgnoreCase);

        var parentLinksByAlias = BuildParentLinksByAlias(entityTrees);

        var cteParentAliases = CollectCteParentAliases(entityTrees, columnsByAlias, aliasesToProcess, parentLinksByAlias);

        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var entitiesProcessed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var queue = new Queue<EntityNodeTree>();

        // The root itself may be the node with data to generate (e.g. a graph-edge entity like
        // CustomerCustomerEdge, which has no FK-based parent/child link into entityTrees - it's
        // connected via GraphMap, not a relational FK - so it can only ever be discovered by
        // seeding it directly here, never via ModelToEntity or parentLinksByAlias).
        queue.Enqueue(rootTree);

        foreach (var link in rootTree.ModelToEntity)
        {
            if (entityTrees.TryGetValue(link.AliasTo, out var seed))
                queue.Enqueue(seed);
        }

        while (queue.Count > 0)
        {
            var current = queue.Dequeue();

            if (!visited.Add(current.Alias))
                continue;

            var eligibleForGeneration =
                aliasesToProcess.Contains(current.Alias) &&
                entitiesProcessed.Add(current.Alias) &&
                !cteParentAliases.Contains(current.Alias);

            if (eligibleForGeneration)
            {
                if (current.IsGraph)
                {
                    AppendGraphMerge(current, selectStatements);
                }
                else if (columnsByAlias.TryGetValue(current.Alias, out var currentColumns) && currentColumns.Count > 0)
                {
                    var fkLinks = CollectFkLinksForTree(current, columnsByAlias, parentLinksByAlias);

                    if (fkLinks.Count > 0)
                    {
                        var cte = BuildCteSql(current, currentColumns, fkLinks);
                        if (!string.IsNullOrEmpty(cte) && !statements.Contains(cte))
                            statements.Add(cte);
                    }
                    else
                    {
                        GenerateUpsert(
                            current,
                            currentColumns,
                            sqlWhereStatement.GetValueOrDefault(current.Alias) ?? string.Empty,
                            statements);
                    }

                    AppendGraphMerge(current, selectStatements);
                }
            }

            foreach (var link in current.EntityChildren)
                EnqueueChild(link, entityTrees, visited, queue);

            foreach (var link in current.EntityChildrenRelated)
                EnqueueChild(link, entityTrees, visited, queue);
        }
    }

    private static void EnqueueChild(
        EntityKey link,
        Dictionary<string, EntityNodeTree> entityTrees,
        HashSet<string> visited,
        Queue<EntityNodeTree> queue)
    {
        var nextAlias = !string.IsNullOrWhiteSpace(link.AliasTo) ? link.AliasTo : link.To;
        if (string.IsNullOrWhiteSpace(nextAlias) || visited.Contains(nextAlias))
            return;

        if (entityTrees.TryGetValue(nextAlias, out var nextTree))
            queue.Enqueue(nextTree);
    }

    private static Dictionary<string, List<(EntityNodeTree ParentTree, EntityKey Link)>> BuildParentLinksByAlias(
        Dictionary<string, EntityNodeTree> entityTrees)
    {
        var result = new Dictionary<string, List<(EntityNodeTree ParentTree, EntityKey Link)>>(
            StringComparer.OrdinalIgnoreCase);

        void Index(EntityNodeTree parentTree, EntityKey link)
        {
            var dependentAlias = link.From;
            if (string.IsNullOrWhiteSpace(dependentAlias)) return;

            if (!result.TryGetValue(dependentAlias, out var list))
            {
                list = new List<(EntityNodeTree ParentTree, EntityKey Link)>();
                result[dependentAlias] = list;
            }

            var alreadyIndexed = list.Any(x =>
                x.ParentTree.Alias.Equals(parentTree.Alias, StringComparison.OrdinalIgnoreCase) &&
                x.Link.ToColumn.Equals(link.ToColumn, StringComparison.OrdinalIgnoreCase));

            if (!alreadyIndexed)
                list.Add((parentTree, link));
        }

        foreach (var parentTree in entityTrees.Values)
        {
            foreach (var link in parentTree.EntityChildren)
                Index(parentTree, link);

            foreach (var link in parentTree.EntityChildrenRelated)
                Index(parentTree, link);
        }

        return result;
    }

    private static HashSet<string> CollectCteParentAliases(
        Dictionary<string, EntityNodeTree> entityTrees,
        Dictionary<string, List<(string Column, string Value)>> columnsByAlias,
        HashSet<string> aliasesToProcess,
        Dictionary<string, List<(EntityNodeTree ParentTree, EntityKey Link)>> parentLinksByAlias)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var alias in aliasesToProcess)
        {
            // entityTrees is keyed by alias already - use a direct lookup instead of the
            // previous fuzzy Name.Matches(alias) scan, which broke down whenever an entity's
            // table Name (e.g. "CustomerCustomerRelationship") didn't equal its role alias
            // (e.g. "CustomerCustomerRelationshipKey").
            if (!entityTrees.TryGetValue(alias, out var tree))
                continue;

            foreach (var fk in CollectFkLinksForTree(tree, columnsByAlias, parentLinksByAlias))
                result.Add(fk.ParentTree.Alias);
        }

        return result;
    }

    private sealed record FkLink(
        EntityNodeTree ParentTree,
        string FkColumn,
        string PkColumn,
        string CteName,
        List<(string Column, string Value)> ParentColumns,
        List<string> OnConflictCols);

    private static List<FkLink> CollectFkLinksForTree(
        EntityNodeTree currentTree,
        Dictionary<string, List<(string Column, string Value)>> columnsByAlias,
        Dictionary<string, List<(EntityNodeTree ParentTree, EntityKey Link)>> parentLinksByAlias)
    {
        var result = new List<FkLink>();

        // Must key off Alias, not Name: parentLinksByAlias is indexed by link.From, which is
        // the dependent's role alias (e.g. "CustomerCustomerRelationshipKey"), not its
        // underlying table Name (e.g. "CustomerCustomerRelationship"). Looking this up by
        // currentTree.Name silently missed every time the alias and table name diverged,
        // which made fkLinks come back empty and fell through to the plain GenerateUpsert
        // (DO NOTHING) path instead of building the parent CTEs.
        if (!parentLinksByAlias.TryGetValue(currentTree.Alias, out var incomingLinks))
            return result;

        foreach (var (parentTree, link) in incomingLinks)
            TryAddFkLink(link, parentTree, columnsByAlias, currentTree, result);

        return result;
    }

    private static void TryAddFkLink(
        EntityKey link,
        EntityNodeTree parentTree,
        Dictionary<string, List<(string Column, string Value)>> columnsByAlias,
        EntityNodeTree currentTree,
        List<FkLink> result)
    {
        // Customer's EF navigations are returned for BOTH InnerCustomerCustomer and
        // OuterCustomerCustomer (GetNavigations doesn't know which role-alias is asking, since
        // both aliases share the same underlying EF entity type), so each role-specific parent
        // sees both the InnerCustomerId and OuterCustomerId FK links. Filter to only the link
        // whose FK column actually matches this parent's role (ModelName), e.g. parentTree
        // "InnerCustomer" should only ever produce the "InnerCustomerId" link, never
        // "OuterCustomerId" too. NOTE: this is a naming-convention filter, not a structural
        // one - the structurally correct fix is to populate EntityKey.AliasProperty from the
        // AddModelToEntity/navigation call and filter on that instead, once it's wired through.
        if (!string.IsNullOrEmpty(parentTree.ModelName) &&
            !link.ToColumn.Contains(parentTree.ModelName, StringComparison.OrdinalIgnoreCase))
            return;

        // columnsByAlias is keyed by role alias (e.g. "InnerCustomerCustomer"), not by the
        // underlying table Name (e.g. "Customer") - both InnerCustomer and OuterCustomer share
        // the same Name, so looking this up by Name would only ever resolve to one shared
        // bucket (or none at all) instead of each role's own column values.
        if (!columnsByAlias.TryGetValue(parentTree.Alias, out var parentColumns) || parentColumns.Count == 0)
            return;

        result.Add(new FkLink(
            ParentTree: parentTree,
            FkColumn: link.ToColumn,
            PkColumn: link.FromColumn,
            // Keyed by Alias, not Name, so InnerCustomer and OuterCustomer each get their own
            // distinct CTE name (cte_InnerCustomerCustomer / cte_OuterCustomerCustomer) instead
            // of colliding on a shared cte_Customer, which Postgres would reject as a duplicate
            // CTE name in the same WITH clause.
            CteName: $"cte_{parentTree.Alias}",
            ParentColumns: parentColumns,
            OnConflictCols: currentTree.UpsertKeys));
    }

    private static string BuildCteSql(
        EntityNodeTree currentTree,
        List<(string Column, string Value)> currentColumns,
        List<FkLink> fkLinks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("WITH");

        var cteTerms = new List<string>(fkLinks.Count);

        foreach (var fk in fkLinks)
        {
            var cols = string.Join(", ", fk.ParentColumns.Select(c => $"\"{c.Column}\""));
            var vals = string.Join(", ", fk.ParentColumns.Select(c => $"'{EscapeValue(c.Value)}'"));

            // FIX: this CTE inserts into fk.ParentTree (e.g. Customer), so its ON CONFLICT
            // target must be the PARENT's own upsert keys (Customer.CustomerKey) - NOT
            // fk.OnConflictCols, which is currentTree.UpsertKeys (the DEPENDENT/child's keys,
            // e.g. CustomerCustomerRelationship.CustomerCustomerRelationshipKey). Those two were
            // previously conflated: fk.OnConflictCols is correctly reused further below for the
            // final INSERT INTO currentTree's own ON CONFLICT clause - that part stays as-is.
            var conflict = string.Join(", ", fk.ParentTree.UpsertKeys.Select(c => $"\"{c}\""));
            var set = string.Join(", ", fk.ParentColumns.Select(c => $"\"{c.Column}\" = EXCLUDED.\"{c.Column}\""));

            cteTerms.Add(
                $"    {fk.CteName} AS (\n" +
                $"        INSERT INTO \"{fk.ParentTree.Schema}\".\"{fk.ParentTree.Name}\" ({cols})\n" +
                $"        VALUES ({vals})\n" +
                $"        ON CONFLICT ({conflict}) DO UPDATE SET {set}\n" +
                $"        RETURNING \"{fk.PkColumn}\"\n" +
                $"    )");
        }

        if (cteTerms.Count == 0)
            return string.Empty;

        sb.Append(string.Join(",\n", cteTerms));
        sb.AppendLine();

        var fkCols = fkLinks.Select(f => f.FkColumn).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var conflictCols = fkLinks[0].OnConflictCols;

        var scalarCols = currentColumns
            .Where(c => !fkCols.Contains(c.Column))
            .ToList();

        var insertCols = new List<string>();
        var selectCols = new List<string>();
        var fromCtes = new List<string>();
        var setCols = new List<string>();

        foreach (var col in scalarCols)
        {
            insertCols.Add($"\"{col.Column}\"");
            selectCols.Add($"'{EscapeValue(col.Value)}' AS \"{col.Column}\"");

            if (!conflictCols.Contains(col.Column))
                setCols.Add($"\"{col.Column}\" = EXCLUDED.\"{col.Column}\"");
        }

        foreach (var fk in fkLinks)
        {
            insertCols.Add($"\"{fk.FkColumn}\"");
            selectCols.Add($"{fk.CteName}.\"{fk.PkColumn}\" AS \"{fk.FkColumn}\"");
            fromCtes.Add(fk.CteName);
            setCols.Add($"\"{fk.FkColumn}\" = EXCLUDED.\"{fk.FkColumn}\"");
        }

        sb.AppendLine($"INSERT INTO \"{currentTree.Schema}\".\"{currentTree.Name}\"");
        sb.AppendLine($"({string.Join(", ", insertCols)})");
        sb.AppendLine($"SELECT {string.Join(", ", selectCols)}");
        sb.AppendLine($"FROM {string.Join(", ", fromCtes)}");
        sb.AppendLine($"ON CONFLICT ({string.Join(", ", conflictCols.Select(c => $"\"{c}\""))})");
        sb.AppendLine($"DO UPDATE SET {string.Join(", ", setCols)};");

        return sb.ToString();
    }

    private static void AppendGraphMerge(EntityNodeTree currentTree, List<string> selectStatements)
    {
        if (currentTree.GraphMap == null)
            return;

        var sql = BuildMergeRelationship(
            currentTree.GraphMap.GraphName,
            currentTree.GraphMap.FromVertex.Label,
            currentTree.GraphMap.FromVertex.KeyColumn,
            "",
            currentTree.GraphMap.ToVertex.Label,
            currentTree.GraphMap.ToVertex.KeyColumn,
            "",
            currentTree.GraphMap.EdgeLabel,
            currentTree.GraphMap.EdgeKeyColumn,
            "");

        if (!selectStatements.Contains(sql))
            selectStatements.Add(sql);
    }

    public static void GenerateUpsert(
        EntityNodeTree currentTree,
        List<(string Column, string Value)> currentColumns,
        string whereClause,
        List<string> statements)
    {
        if (currentColumns.Count == 0)
            return;

        var colNames = string.Join(", ", currentColumns.Select(c => $"\"{c.Column}\""));
        var colVals = string.Join(", ", currentColumns.Select(c => $"'{EscapeValue(c.Value)}'"));

        var sql =
            $"INSERT INTO \"{currentTree.Schema}\".\"{currentTree.Name}\" ({colNames}) " +
            $"VALUES ({colVals}) " +
            $"ON CONFLICT ({string.Join(", ", currentTree.UpsertKeys.Select(k => $"\"{k}\""))}) " +
            $"DO NOTHING {whereClause};";

        if (!statements.Contains(sql))
            statements.Add(sql);
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
        var setClause = edgeProperties == null
            ? ""
            : "SET " + string.Join(", ", edgeProperties.Select(p => $"r.{p.Key} = '{EscapeValue(p.Value)}'"));

        return $@"
;CREATE TEMP TABLE temp_merge AS SELECT 1
FROM ag_catalog.cypher(
'{graphName}',
$$
MERGE (a:{fromLabel} {{ {fromKeyColumn}: '{EscapeValue(fromValue)}' }})
MERGE (b:{toLabel} {{ {toKeyColumn}: '{EscapeValue(toValue)}' }})
MERGE (a)-[r:{edgeLabel}]->(b)
{setClause}
RETURN r
$$
) AS (r text); DROP TABLE temp_merge;";
    }

    private static string EscapeValue(string value)
        => value?.Replace("'", "''") ?? string.Empty;
}