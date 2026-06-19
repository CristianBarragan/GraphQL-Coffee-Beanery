using System.Text;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    /// <summary>
    /// Builds a single joined SELECT from a QueryGraphWalker.Result. Traversal now follows
    /// the Result's RowKey tree (ParentRowKeyByRowKey / FieldNameByRowKey) rather than the
    /// FK-graph's plain EntityNodeTree.Alias - this is required for self-joins (e.g. a
    /// CustomerCustomerRelationship with two distinct FK links to "Customer", one for
    /// innerCustomer and one for outerCustomer): the same Alias can legitimately appear at
    /// more than one RowKey, and each occurrence must be joined into the SQL as its own
    /// distinctly-aliased table, using the EntityKey whose AliasProperty matches the specific
    /// GraphQL field name that produced that RowKey (NOT just "first EntityKey whose AliasTo
    /// matches", which silently picks the wrong link - or drops the second occurrence
    /// entirely - whenever an alias is reachable by more than one navigation).
    ///
    /// PkColumnOrder / SplitOn / Types are now one-per-RowKey (not one-per-alias) for the same
    /// reason: Dapper needs a distinct slice per actual table instance in the result set, and
    /// two instances of the same entity type still need two slices.
    /// </summary>
    public static class SqlSelectStatementBuilder
    {
        public sealed class Result
        {
            public string Sql { get; init; } = string.Empty;

            // One entry per RowKey instance (not per alias) - e.g. for a self-join this has
            // two entries that share the same Type/real-alias but different SqlAlias/RowKey.
            public List<string> RowKeyOrder { get; init; } = new();

            // Real NodeRegistry alias backing each RowKey, same order as RowKeyOrder.
            public List<string> AliasOrder { get; init; } = new();

            // The actual SQL alias each RowKey was projected/joined under (unique even when
            // AliasOrder has the same alias more than once - see BuildSqlAlias).
            public List<string> SqlAliasOrder { get; init; } = new();

            public List<Type> Types { get; init; } = new();

            // Real PK column name per RowKey, same order as RowKeyOrder. Used to build SplitOn.
            public List<string> PkColumnOrder { get; init; } = new();

            public string SplitOn => string.Join(",", PkColumnOrder.Skip(1));
        }

        public static Result Build(
            Dictionary<string, EntityNodeTree> entityTrees,
            QueryGraphWalker.Result queryData,
            EntityNodeTree rootTree,
            Dictionary<string, string> sqlWhereStatement,
            Dictionary<string, string> sqlOrderStatement)
        {
            var visitedRowKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var selectCols = new List<string>();
            var joins = new StringBuilder();

            var orderedRowKeys = new List<string>();
            var orderedAliases = new List<string>();
            var orderedSqlAliases = new List<string>();
            var orderedTypes = new List<Type>();
            var orderedPkColumns = new List<string>();

            // rowKey -> the SQL alias it was actually emitted under. Needed so a child's join
            // condition references its parent's *SQL* alias, not the bare entity alias (which
            // may be ambiguous when the parent itself is a repeated instance further up).
            var sqlAliasByRowKey = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // Disambiguates repeated real aliases (e.g. "Customer" appearing twice) into
            // distinct, valid SQL identifiers: "Customer", "Customer_2", "Customer_3", ...
            var aliasUseCount = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            string BuildSqlAlias(string alias)
            {
                var count = aliasUseCount.TryGetValue(alias, out var c) ? c + 1 : 1;
                aliasUseCount[alias] = count;
                return count == 1 ? alias : $"{alias}_{count}";
            }

            void EmitColumnsFor(string rowKey, EntityNodeTree tree, string sqlAlias)
            {
                orderedRowKeys.Add(rowKey);
                orderedAliases.Add(tree.Alias);
                orderedSqlAliases.Add(sqlAlias);
                orderedTypes.Add(tree.EntityType);

                if (!queryData.ColumnsByRowKey.TryGetValue(rowKey, out var cols) || cols.Count == 0)
                {
                    selectCols.Add($"\"{sqlAlias}\".\"Id\" AS \"{sqlAlias}_Id\"");
                    orderedPkColumns.Add($"{sqlAlias}_Id");
                    return;
                }

                // PK first (guaranteed by QueryGraphWalker.EnsurePrimaryKey). Projected under
                // an alias-qualified column name (e.g. "Customer_2_CustomerKey") so that when
                // the same real column name repeats across two instances of the same entity,
                // Dapper's name-based binding and the SplitOn boundary both stay unambiguous.
                var pkColumn = cols[0];
                var pkOutName = sqlAlias == tree.Alias ? pkColumn : $"{sqlAlias}_{pkColumn}";
                selectCols.Add($"\"{sqlAlias}\".\"{pkColumn}\" AS \"{pkOutName}\"");
                orderedPkColumns.Add(pkOutName);

                foreach (var col in cols.Skip(1))
                {
                    var outName = sqlAlias == tree.Alias ? col : $"{sqlAlias}_{col}";
                    selectCols.Add($"\"{sqlAlias}\".\"{col}\" AS \"{outName}\"");
                }
            }

            // Children of a given parent RowKey, in the order QueryGraphWalker discovered them.
            var childRowKeysByParent = queryData.RowKeyOrder
                .Where(rk => !string.IsNullOrEmpty(queryData.ParentRowKeyByRowKey.GetValueOrDefault(rk)))
                .GroupBy(rk => queryData.ParentRowKeyByRowKey[rk], StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            void Visit(string rowKey, EntityNodeTree tree, string sqlAlias)
            {
                if (!visitedRowKeys.Add(rowKey))
                    return;

                sqlAliasByRowKey[rowKey] = sqlAlias;
                EmitColumnsFor(rowKey, tree, sqlAlias);

                if (!childRowKeysByParent.TryGetValue(rowKey, out var childRowKeys))
                    return;

                foreach (var childRowKey in childRowKeys)
                {
                    var childAlias = queryData.AliasByRowKey[childRowKey];
                    var fieldName = queryData.FieldNameByRowKey[childRowKey];

                    if (!entityTrees.TryGetValue(childAlias, out var childTree))
                        continue;

                    // Pick the EntityKey that corresponds to THIS specific navigation, not
                    // just any link whose AliasTo matches - AliasProperty is the FK link's
                    // own navigation/field name, the same thing NodeRegistry.ChildAliasByField
                    // keys (ParentAlias, FieldName) on. This is what makes innerCustomer and
                    // outerCustomer resolve to two different links instead of the same one.
                    var link = tree.EntityChildren.Concat(tree.EntityChildrenRelated)
                        .FirstOrDefault(l =>
                            string.Equals(l.AliasTo, childAlias, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(l.AliasProperty, fieldName, StringComparison.OrdinalIgnoreCase));

                    // Fall back to alias-only match for callers/paths where AliasProperty
                    // wasn't populated (e.g. non-duplicated navigations) - preserves old
                    // behavior for the common case while still fixing the self-join case.
                    link ??= tree.EntityChildren.Concat(tree.EntityChildrenRelated)
                        .FirstOrDefault(l => string.Equals(l.AliasTo, childAlias, StringComparison.OrdinalIgnoreCase));

                    if (link is null)
                        continue;

                    var childSqlAlias = BuildSqlAlias(childAlias);

                    joins.AppendLine(
                        $"LEFT JOIN \"{childTree.Schema}\".\"{childTree.Name}\" \"{childSqlAlias}\" " +
                        $"ON \"{childSqlAlias}\".\"{link.FromColumn}\" = \"{sqlAlias}\".\"{link.ToColumn}\"");

                    Visit(childRowKey, childTree, childSqlAlias);
                }
            }

            var rootSqlAlias = BuildSqlAlias(rootTree.Alias);
            Visit(rootTree.Alias, rootTree, rootSqlAlias);

            var sb = new StringBuilder();
            sb.Append("SELECT DISTINCT ").AppendLine(string.Join(", ", selectCols));
            sb.AppendLine($"FROM \"{rootTree.Schema}\".\"{rootTree.Name}\" \"{rootSqlAlias}\"");
            sb.Append(joins);

            // WHERE/ORDER BY are still keyed by real entity alias upstream (SqlWhereCompiler/
            // SqlOrderCompiler), so they apply against the FIRST sql alias used for that real
            // alias. This is unchanged from prior behavior for non-repeated aliases; a
            // self-joined alias's second instance currently can't be targeted independently
            // by WHERE/ORDER BY - flagged below.
            var whereParts = sqlWhereStatement
                .Where(kv => orderedAliases.Contains(kv.Key, StringComparer.OrdinalIgnoreCase))
                .Select(kv => kv.Value.Replace("~", sqlAliasByRowKey.FirstOrDefault(p =>
                    string.Equals(queryData.AliasByRowKey.GetValueOrDefault(p.Key), kv.Key, StringComparison.OrdinalIgnoreCase)).Value ?? kv.Key))
                .ToList();

            if (whereParts.Count > 0)
                sb.AppendLine("WHERE " + string.Join(" AND ", whereParts));

            if (sqlOrderStatement.Count > 0)
            {
                var orderSql = string.Join(", ", sqlOrderStatement.Select(kv => kv.Value.Replace("~*~", $"\"{kv.Key}\"")));
                sb.AppendLine("ORDER BY " + orderSql);
            }

            return new Result
            {
                Sql = sb.ToString(),
                RowKeyOrder = orderedRowKeys,
                AliasOrder = orderedAliases,
                SqlAliasOrder = orderedSqlAliases,
                Types = orderedTypes,
                PkColumnOrder = orderedPkColumns
            };
        }
    }
}