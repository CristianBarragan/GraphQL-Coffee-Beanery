using System;
using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using MoreLinq;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlSelectBuilderOld
    {
        public static (string rootQuery, OrderedDictionary<string, Type> splitOnDapper, List<string> aliasesOrdered) Build(
            NodeTree nodeTree,
            Dictionary<string, SqlNode> nodeDict,
            Dictionary<string, SqlNode> edgeDict,
            string wrapperEntityName,
            Dictionary<string, string> sqlWhereStatement,
            OrderedDictionary<string, Type> splitOnDapper,
            OrderedDictionary<string, string> aliases,
            bool transformedToParent)
        {
            var sqlQueryStructures   = new Dictionary<string, SqlQueryStructure>(StringComparer.OrdinalIgnoreCase);
            var childrenSqlStatement = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var entityOrder          = new List<string>();

            // Merge edge nodes into node dictionary
            foreach (var edgeKey in edgeDict)
            {
                if (!nodeDict.ContainsKey(edgeKey.Key))
                    nodeDict.Add(edgeKey.Key, edgeKey.Value);
            }

            var nodeModel = SqlNodeRegistry.ModelTrees[wrapperEntityName];

            foreach (var fieldMap in nodeTree.Mapping.Where(a => nodeTree.ModelToEntityLinks.Any(b => b.FromColumn.Matches(a.SourceName))))
            {
                var modelTree = SqlNodeRegistry.ModelTrees[fieldMap.SourceModel];
                var entityTree = modelTree.IsEntity ? modelTree : SqlNodeRegistry.EntityTrees[fieldMap.DestinationEntity];
                
                GenerateQuery(
                    SqlNodeRegistry.EntityTrees,
                    SqlNodeRegistry.ModelTrees,
                    SqlNodeRegistry.EntityTypes,
                    SqlNodeRegistry.EntityNodes,
                    SqlNodeRegistry.ModelNodes,
                    nodeDict,
                    sqlWhereStatement,
                    entityTree,
                    modelTree,
                    childrenSqlStatement,
                    wrapperEntityName,
                    sqlQueryStructures,
                    splitOnDapper,
                    aliases,
                    entityOrder,
                    new List<string>(),
                    new List<string>(),
                    new List<string>());
            }

            if (sqlQueryStructures.Count == 0)
                return (string.Empty, default, default);

            // ── Resolve root query and columns ────────────────────────────────
            // wrapperEntityName may be a graph type (e.g. CustomerCustomerEdge)
            // that has no entry in sqlQueryStructures. Walk entityOrder in reverse
            // to find the outermost assembled node that has actual columns.
            string rootQuery = sqlQueryStructures.Last().Value.Query;
            List<string> rootColumns = sqlQueryStructures.Last().Value.Columns;

            // if (sqlQueryStructures.TryGetValue(wrapperEntityName, out var rootStructure)
            //     && rootStructure.Columns.Count > 0)
            // {
            //     rootQuery   = rootStructure.Query;
            //     rootColumns = rootStructure.Columns;
            // }
            // else
            // {
            //     // Walk entityOrder in reverse — last visited = outermost JOIN
            //     SqlQueryStructure fallback = null;
            //     for (int i = entityOrder.Count - 1; i >= 0; i--)
            //     {
            //         if (sqlQueryStructures.TryGetValue(entityOrder[i], out var candidate)
            //             && candidate.Columns.Count > 0)
            //         {
            //             fallback = candidate;
            //             break;
            //         }
            //     }
            //
            //     // Last resort: structure with most columns
            //     fallback ??= sqlQueryStructures
            //         .OrderByDescending(s => s.Value.Columns.Count)
            //         .First().Value;
            //
            //     rootQuery   = fallback.Query;
            //     rootColumns = fallback.Columns;
            //
            //     Console.ForegroundColor = ConsoleColor.Yellow;
            //     Console.WriteLine($"[WARN] '{wrapperEntityName}' not in sqlQueryStructures, using fallback '{fallback.Alias}'");
            //     Console.ResetColor();
            // }

            // Debug output
            Console.WriteLine("=== ROOT QUERY ALIAS LOOKUP ===");
            Console.WriteLine($"wrapperEntityName: {wrapperEntityName}");
            Console.WriteLine("=== ALL sqlQueryStructures KEYS ===");
            foreach (var key in sqlQueryStructures.Keys)
                Console.WriteLine($"  key: '{key}'");
            Console.WriteLine($"=== ROOT QUERY (first 300 chars) ===");
            Console.WriteLine(rootQuery.Length > 300 ? rootQuery[..300] : rootQuery);
            Console.WriteLine("=== ROOT COLUMNS ===");
            foreach (var col in rootColumns)
                Console.WriteLine($"  {col}");
            Console.WriteLine("=== SPLITON BEFORE REBUILD ===");
            foreach (var kvp in splitOnDapper)
                Console.WriteLine($"{kvp.Key} -> {kvp.Value?.Name}");

            // ── Rebuild splitOn strictly by actual SELECT column order ─────────
            RebuildSplitOnByColumnOrder(rootColumns, splitOnDapper, aliases);

            // Reverse alias ordering for Dapper multi-map
            var aliasesOrdered = aliases.Reverse().Select(a => a.Value).ToList();

            Console.WriteLine("=== FINAL ALIASES ORDERED ===");
            foreach (var a in aliasesOrdered)
                Console.WriteLine(a);

            return (rootQuery, splitOnDapper, aliasesOrdered);
        }

        /// <summary>
        /// Rebuilds splitOnDapper and aliases so their order matches the actual
        /// left-to-right column order in the final SQL SELECT.
        /// Entries whose Id column does not appear in the SELECT are dropped —
        /// this removes unrequested nodes (e.g. OuterCustomer when not queried).
        /// </summary>
        private static void RebuildSplitOnByColumnOrder(
            List<string> allSelectColumns,
            OrderedDictionary<string, Type> splitOnDapper,
            OrderedDictionary<string, string> aliases)
        {
            if (allSelectColumns.Count == 0)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] RebuildSplitOnByColumnOrder called with empty column list — splitOn unchanged");
                Console.ResetColor();
                return;
            }

            var idEntries    = splitOnDapper.ToList();
            var aliasEntries = aliases.ToList();

            var ordered = idEntries
                .Select(kvp =>
                {
                    // Match on the AS alias part at end of column string
                    // Column format: TableAlias."Col" AS "Col__"
                    var searchToken = $"AS \"{kvp.Key}\"";

                    var match = allSelectColumns
                        .Select((col, idx) => new { col, idx })
                        .FirstOrDefault(x => x.col.EndsWith(searchToken));

                    var aliasVal = aliasEntries
                        .FirstOrDefault(a => a.Key == kvp.Key).Value ?? kvp.Key;

                    return new { kvp.Key, kvp.Value, aliasVal, pos = match?.idx };
                })
                .Where(x =>
                {
                    if (x.pos == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine($"[SPLITON] Dropping '{x.Key}' ({x.aliasVal}) — not in root SELECT");
                        Console.ResetColor();
                        return false;
                    }
                    return true;
                })
                .OrderBy(x => x.pos)
                .ToList();

            splitOnDapper.Clear();
            aliases.Clear();

            Console.WriteLine("=== REBUILT SPLITON ===");
            foreach (var entry in ordered)
            {
                Console.WriteLine($"{entry.Key} -> {entry.aliasVal} (pos {entry.pos})");
                splitOnDapper.Add(entry.Key, entry.Value);
                aliases.Add(entry.Key, entry.aliasVal);
            }
        }

        private static void GenerateQuery(
            Dictionary<string, NodeTree> entityTrees,
            Dictionary<string, NodeTree> modelTrees,
            List<Type> entityTypes,
            Dictionary<string, SqlNode> linkEntityDictionaryTree,
            Dictionary<string, SqlNode> linkModelDictionaryTree,
            Dictionary<string, SqlNode> sqlStatementNodes,
            Dictionary<string, string> sqlWhereStatement,
            NodeTree currentEntityTree,
            NodeTree currentModelTree,
            Dictionary<string, string> childrenSqlStatement,
            string rootEntityName,
            Dictionary<string, SqlQueryStructure> sqlQueryStructures,
            OrderedDictionary<string, Type> splitOnDapper,
            OrderedDictionary<string, string> aliases,
            List<string> entityOrder,
            List<string> parentVisitedEntities,
            List<string> childVisitedEntities,
            List<string> generatedQueries)
        {
            var currentTree = currentEntityTree;
            
            var alias = string.IsNullOrEmpty(currentTree.Alias) ? currentTree.Name : currentTree.Alias;

            if (parentVisitedEntities.Contains(alias) ||
                sqlQueryStructures.Any(a => a.Value.Alias == currentTree.Alias))
                return;

            parentVisitedEntities.Add(alias);
            entityOrder.Add(currentTree.Alias);

            var allChildren = (currentEntityTree.Children ?? new List<LinkKey>())
                .Concat(currentEntityTree.RelatedChildren ?? new List<LinkKey>())
                .ToList();

            // ── Recurse into children first (post-order) ──────────────────────
            // No pre-filtering here — let all children recurse so sqlQueryStructures
            // is fully populated before we assemble the parent JOIN.
            foreach (var treeAlias in entityTrees.Where(a => a.Value.Name.Matches(currentTree.Name)))
            {
                if (sqlQueryStructures.Any(a => a.Value.Alias.Matches(treeAlias.Value.Alias)))
                    continue;

                var treeChildren = (treeAlias.Value.Children ?? new List<LinkKey>())
                    .Concat(treeAlias.Value.RelatedChildren ?? new List<LinkKey>())
                    .ToList();

                foreach (var child in treeChildren.Where(c => !splitOnDapper.Keys.Contains(c.To)))
                {
                    if (!allChildren.Any(k => k.To.Matches(child.To)))
                        continue;

                    if (!entityTrees.ContainsKey(child.To))
                        continue;

                    parentVisitedEntities = new List<string>(parentVisitedEntities) { currentTree.Name };

                    if (currentTree.Parents?.Count > 0 && !string.IsNullOrEmpty(currentTree.Parents[0].To))
                        parentVisitedEntities.Add(currentTree.Parents[0].To);

                    if (currentTree.RelatedParents?.Count > 0 && !string.IsNullOrEmpty(currentTree.RelatedParents[0].To))
                        parentVisitedEntities.Add(currentTree.RelatedParents[0].To);

                    GenerateQuery(
                        entityTrees,
                        modelTrees,
                        entityTypes,
                        linkEntityDictionaryTree,
                        linkModelDictionaryTree,
                        sqlStatementNodes,
                        sqlWhereStatement,
                        entityTrees[treeAlias.Value.Alias],
                        modelTrees[child.To],
                        childrenSqlStatement,
                        rootEntityName,
                        sqlQueryStructures,
                        splitOnDapper,
                        aliases,
                        entityOrder,
                        parentVisitedEntities,
                        childVisitedEntities,
                        generatedQueries);
                }
            }

            // ── Generate columns for current node ──────────────────────────────
            var (ownColumns, parentColumns, currentColumns) = GenerateEntityQuery(
                entityTrees,
                modelTrees,
                linkEntityDictionaryTree,
                sqlStatementNodes,
                currentEntityTree,
                currentModelTree,
                sqlQueryStructures,
                sqlWhereStatement,
                childrenSqlStatement,
                rootEntityName,
                generatedQueries);

            if (ownColumns.Count == 0)
                return;

            var allSelectColumns = new List<string>(ownColumns);
            var joinClauses      = new List<string>();

            // ── Bubble up child columns into parentColumns ────────────────────
            // Only include children that have requested fields (more than just Id)
            foreach (var child in allChildren)
            {
                if (string.IsNullOrEmpty(child.To) ||
                    !sqlQueryStructures.TryGetValue(child.To, out var childStructure))
                    continue;

                // Skip children that were built but had no requested fields
                // HasRequestedFields is true when currentColumns.Count > 1
                // if (!childStructure.HasRequestedFields)
                //     continue;

                var childAlias = childStructure.Alias;
                var childTree  = entityTrees[child.To];

                foreach (var col in childStructure.Columns)
                {
                    if (!parentColumns.Contains(col))
                        parentColumns.Add(col);
                }

                if (!string.IsNullOrEmpty(child.ToColumn))
                {
                    var fkColumn = $"{childAlias}.\"{child.ToColumn}\" AS \"{child.ToColumn}{childTree.Id}\"";
                    if (!parentColumns.Contains(fkColumn))
                        parentColumns.Add(fkColumn);
                }

                bool isRelevantChild = child.From.Matches(currentTree.Alias);
                if (!isRelevantChild) continue;

                foreach (var propagatedColumn in childStructure.ParentColumns)
                {
                    if (!propagatedColumn.StartsWith(childAlias + "."))
                        continue;

                    if (!parentColumns.Contains(propagatedColumn))
                        parentColumns.Add(propagatedColumn);
                }
            }

            // ── Build JOIN clauses and propagate columns ──────────────────────
            // Only JOIN children that have requested fields
            foreach (var child in allChildren)
            {
                if (string.IsNullOrEmpty(child.To) ||
                    !sqlQueryStructures.TryGetValue(child.To, out var childStructure) ||
                    generatedQueries.Contains(childStructure.Query))
                    continue;

                // Skip children that were built but had no requested fields
                // if (!childStructure.HasRequestedFields)
                //     continue;

                childStructure.Visited = true;
                generatedQueries.Add(childStructure.Query);

                var childTree  = entityTrees[child.To];
                var childAlias = childStructure.Alias;
                var joinType   = childStructure.SqlNodeType == SqlNodeType.Edge ? "JOIN" : "LEFT JOIN";

                var exportedChildColumn = child.ToColumn.ToSnakeCase(childTree.Id);

                joinClauses.Add(
                    $" {joinType} ( {childStructure.Query} ) {childAlias}" +
                    $" ON {alias}.\"{child.FromColumn}\" = {childAlias}.\"{exportedChildColumn}\"");

                foreach (var col in childStructure.Columns)
                {
                    var parts = col.Split(new[] { " AS " }, StringSplitOptions.None);
                    if (parts.Length != 2)
                        continue;

                    var aliasSelect     = parts[1];
                    var columnWithAlias = $"{childAlias}.\"{aliasSelect.Trim('"')}\" AS {aliasSelect}";

                    if (!allSelectColumns.Contains(columnWithAlias))
                        allSelectColumns.Add(columnWithAlias);
                }

                foreach (var parentCol in childStructure.ParentColumns)
                {
                    var columnWithAlias = parentCol.Replace("~", childAlias);
                    if (!parentColumns.Contains(columnWithAlias))
                        parentColumns.Add(columnWithAlias);
                }
            }

            var idSnake = "Id".ToSnakeCase(currentTree.Id);

            // ── WHERE clause ──────────────────────────────────────────────────
            var whereClause = string.Empty;
            if (sqlWhereStatement.TryGetValue(currentTree.Name, out var whereStatement) && !string.IsNullOrEmpty(whereStatement))
            {
                whereStatement = whereStatement.Replace("~", currentTree.Name);
                foreach (var field in whereStatement.Split("\""))
                {
                    if (currentColumns.Any(c => c.Value?.Column.Matches(field) == true))
                        whereStatement = whereStatement.Replace(field,
                            currentTree.Name.Matches(rootEntityName)
                                ? field
                                : field.ToSnakeCase(currentTree.Id));
                }
                whereClause = $" WHERE {whereStatement}";
            }

            // ── Assemble final query ───────────────────────────────────────────
            var select       = string.Join(",", allSelectColumns.Distinct());
            var queryBuilder =
                $"SELECT {select} FROM \"{currentTree.Schema}\".\"{currentTree.Name}\" {alias}" +
                string.Join("", joinClauses) +
                whereClause;

            // ── SplitOn: just append — Build() will re-sort and drop extras ───
            if (!splitOnDapper.ContainsKey(idSnake) && allSelectColumns.Count > 0)
            {
                var entityType = entityTypes.FirstOrDefault(e => e.Name.Matches(currentTree.Name));
                splitOnDapper.Add(idSnake, entityType);
                aliases.Add(idSnake, currentTree.Alias);
            }

            // ── Register SqlQueryStructure ─────────────────────────────────────
            var currentModelNode = linkModelDictionaryTree.FirstOrDefault(a => a.Key.Contains(currentTree.Name));
            var currentNode      = linkEntityDictionaryTree.FirstOrDefault(a => a.Key.Contains(currentModelNode.Value?.Table));

            if (currentNode.Key == null)
                return;

            var sqlStructure = new SqlQueryStructure
            {
                Id                 = currentTree.Id,
                SqlNodeType        = currentNode.Value.SqlNodeTypes.First(),
                SqlNode            = currentNode.Value,
                Query              = queryBuilder,
                Alias              = currentTree.Alias,
                Columns            = allSelectColumns.Distinct().ToList(),
                ParentColumns      = parentColumns.Distinct().ToList(),
                LinkKeys           = currentNode.Value?.LinkKeys?.ToList() ?? new List<LinkKey>(),
                // True when this node had more than just Id selected —
                // used to filter unrequested nodes from JOIN and bubble-up
                HasRequestedFields = currentColumns.Count > 1,
            };

            if (!sqlQueryStructures.ContainsKey(currentTree.Alias))
                sqlQueryStructures.Add(currentTree.Alias, sqlStructure);
        }

        private static (List<string> ownColumns, List<string> parentColumns,
            List<KeyValuePair<string, SqlNode>> currentColumns)
            GenerateEntityQuery(
                Dictionary<string, NodeTree> entityTrees,
                Dictionary<string, NodeTree> modelTrees,
                Dictionary<string, SqlNode> linkEntityDictionaryTree,
                Dictionary<string, SqlNode> sqlStatementNodes,
                NodeTree currentEntityTree,
                NodeTree currentModelTree,
                Dictionary<string, SqlQueryStructure> sqlQueryStructures,
                Dictionary<string, string> sqlWhereStatement,
                Dictionary<string, string> childrenSqlStatement,
                string rootEntityName,
                List<string> generatedQueries)
        {
            var ownColumns     = new List<string>();
            var parentColumns  = new List<string>();
            var currentColumns = new List<KeyValuePair<string, SqlNode>>();

            Console.WriteLine($"[GenerateEntityQuery] currentTree.Alias='{currentEntityTree.Alias}' currentTree.Name='{currentEntityTree.Name}'");

            // Always include Id
            var idKey = linkEntityDictionaryTree.Keys
                .FirstOrDefault(a => a.Contains($"{currentEntityTree.Alias}~{currentEntityTree.Name}~Id"));

            if (idKey == null)
                return (ownColumns, parentColumns, currentColumns);

            var idNode = linkEntityDictionaryTree[idKey];
            idNode.SqlNodeTypes.Clear();
            idNode.SqlNodeTypes.Add(SqlNodeType.Node);
            currentColumns.Insert(0, new KeyValuePair<string, SqlNode>(idKey, idNode));

            // Add requested fields matching this node's alias or entity name
            currentColumns.AddRange(sqlStatementNodes
                .Where(k => (k.Key.Split('~')[0].Matches(currentModelTree.Alias) && k.Key.Split('~')[1].Matches(currentModelTree.Name)) || 
                            (k.Key.Split('~')[0].Matches(currentEntityTree.Alias) && k.Key.Split('~')[1].Matches(currentEntityTree.Name)) ));
                // {
                //     var parts = k.Key.Split('~');
                //     var currentTree = !currentModelTree.IsEntity ? currentModelTree : currentEntityTree;
                //     
                //     if (parts.Length < 3) return false;
                //     return k.Value.LinkKeys.Any(b =>
                //         b.From.Matches(parts[1]) &&
                //         parts[0].Matches(currentEntityTree.Alias) &&
                //         (b.To.Matches(currentTree.Name) ||
                //             (b.From.Matches(currentModelTree.Name) && k.Value.Table.Matches(currentModelTree.Name))) &&
                //         (b.FromColumn.Matches(parts[2]) || b.FromColumn.Matches(k.Value.Column)));
                // }));

            Console.WriteLine($"  currentColumns.Count after field match: {currentColumns.Count}");

            var allChildren = (currentEntityTree.Children ?? new List<LinkKey>())
                .Concat(currentEntityTree.RelatedChildren ?? new List<LinkKey>())
                .ToList();

            // Only return early if no children are in sqlQueryStructures AND
            // no fields were requested (only Id)
            var hasChildrenInStructures = allChildren.Any(a =>
                sqlQueryStructures.TryGetValue(a.To, out var cs));
                                          // && cs.HasRequestedFields);

            Console.WriteLine($"  allChildren: {string.Join(", ", allChildren.Select(c => c.To))}");
            Console.WriteLine($"  hasChildrenInStructures: {hasChildrenInStructures}");

            if (!hasChildrenInStructures && currentColumns.Count == 1)
                return (ownColumns, parentColumns, currentColumns);

            var idSnake = "Id".ToSnakeCase(currentEntityTree.Id);

            // ── Own SELECT columns ─────────────────────────────────────────────
            foreach (var col in currentColumns.DistinctBy(c => c.Key))
            {
                var fieldName = col.Value.Column;

                // Use model property name as alias so Dapper maps correctly
                // e.g. DB column "FirstName" → model property "FirstNaming"
                var fieldMap      = currentEntityTree.Mapping?.FirstOrDefault(f => f.DestinationName == fieldName);
                var modelPropName = fieldMap?.SourceName ?? fieldName;
                var snakeField    = modelPropName.ToSnakeCase(currentEntityTree.Id);

                ownColumns.Add($"{currentEntityTree.Alias}.\"{fieldName}\" AS \"{snakeField}\"");
            }

            // Export FK columns needed by parent joins
            foreach (var parent in (currentEntityTree.Parents ?? new List<LinkKey>())
                     .Concat(currentEntityTree.RelatedParents ?? new List<LinkKey>()))
            {
                if (string.IsNullOrEmpty(parent.FromColumn))
                    continue;

                var fkAlias  = parent.FromColumn.ToSnakeCase(currentEntityTree.Id);
                var fkColumn = $"{currentEntityTree.Alias}.\"{parent.FromColumn}\" AS \"{fkAlias}\"";

                if (!ownColumns.Contains(fkColumn))
                    ownColumns.Add(fkColumn);

                if (!parentColumns.Contains($"~.\"{fkAlias}\" AS \"{fkAlias}\""))
                    parentColumns.Add($"~.\"{fkAlias}\" AS \"{fkAlias}\"");
            }

            // ── Parent columns: always expose Id ──────────────────────────────
            // Must use the same alias as ownColumns — derive from model property name
            var idFieldMap    = currentEntityTree.Mapping?.FirstOrDefault(f => f.DestinationName == "Id");
            var idModelName   = idFieldMap?.SourceName ?? "Id";
            var idSnakeParent = idModelName.ToSnakeCase(currentEntityTree.Id);

            parentColumns.Add($"~.\"{idSnakeParent}\" AS \"{idSnakeParent}\"");

            foreach (var col in currentColumns.DistinctBy(c => c.Key).Skip(1))
            {
                var fieldName     = col.Value.Column;
                var fieldMap      = currentEntityTree.Mapping?.FirstOrDefault(f => f.DestinationName == fieldName);
                var modelPropName = fieldMap?.SourceName ?? fieldName;
                var snakeField    = modelPropName.ToSnakeCase(currentEntityTree.Id);
                parentColumns.Add($"~.\"{snakeField}\" AS \"{snakeField}\"");
            }

            return (ownColumns, parentColumns, currentColumns);
        }
    }
}