using System;
using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using MoreLinq;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlSelectBuilder
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
            
            var nodeEntity =
                SqlNodeRegistry.EntityTrees[wrapperEntityName];

            // if (nodeTreeKeyValuePair.Value == null)
            // {
            //     nodeTreeKeyValuePair = SqlNodeRegistry.EntityTrees.FirstOrDefault(a => a.Value.ModelToEntityLinks[0].From.Matches(wrapperEntityName));
            // }
            
            // foreach (var entityLink in nodeEntity.ModelToEntityLinks)
            // {
                // Generate the SQL recursively
                GenerateQuery(
                    SqlNodeRegistry.EntityTrees,
                    SqlNodeRegistry.EntityTypes,
                    SqlNodeRegistry.EntityNodes,
                    SqlNodeRegistry.ModelNodes,
                    nodeDict,
                    sqlWhereStatement,
                    nodeEntity,
                    childrenSqlStatement,
                    wrapperEntityName,
                    sqlQueryStructures,
                    splitOnDapper,
                    aliases,
                    entityOrder,
                    new List<string>(),
                    new List<string>(),
                    new List<string>());    
            // }

            if (sqlQueryStructures.Count == 0)
                return (string.Empty, default, default);

            // Reverse alias ordering
            var aliasesOrdered = aliases.Reverse().Select(a => a.Value).ToList();

            var rootQuery = sqlQueryStructures.TryGetValue(wrapperEntityName, out var rootStructure)
                ? rootStructure.Query
                : sqlQueryStructures.LastOrDefault().Value?.Query ?? string.Empty;

            return (rootQuery, splitOnDapper, aliasesOrdered);
        }

        private static void GenerateQuery(
            Dictionary<string, NodeTree> entityTrees,
            List<Type> entityTypes,
            Dictionary<string, SqlNode> linkEntityDictionaryTree,
            Dictionary<string, SqlNode> linkModelDictionaryTree,
            Dictionary<string, SqlNode> sqlStatementNodes,
            Dictionary<string, string> sqlWhereStatement,
            NodeTree currentTree,
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
            var alias = string.IsNullOrEmpty(currentTree.Alias) ? currentTree.Name : currentTree.Alias;

            if (parentVisitedEntities.Contains(alias) ||
                sqlQueryStructures.Any(a => a.Value.Alias == currentTree.Alias))
                return;

            parentVisitedEntities.Add(alias);
            currentTree = entityTrees[alias];
            entityOrder.Add(currentTree.Name);

            var allChildren = (currentTree.Children ?? new List<LinkKey>())
                .Concat(currentTree.RelatedChildren ?? new List<LinkKey>())
                .ToList();

            // ── Recurse into children first (post-order) ─────────────────────
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
                        entityTypes,
                        linkEntityDictionaryTree,
                        linkModelDictionaryTree,
                        sqlStatementNodes,
                        sqlWhereStatement,
                        entityTrees[child.To],
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

            // ── Generate columns for current node ─────────────────────────────
            var (ownColumns, parentColumns, currentColumns) = GenerateEntityQuery(
                entityTrees,
                linkEntityDictionaryTree,
                sqlStatementNodes,
                currentTree,
                sqlQueryStructures,
                sqlWhereStatement,
                childrenSqlStatement,
                rootEntityName,
                generatedQueries);

            if (ownColumns.Count == 0)
                return;

            var allSelectColumns = new List<string>(ownColumns);
            var joinClauses = new List<string>();

            // ── Bubble up child columns into parentColumns ───────────────────
            foreach (var child in allChildren)
            {
                if (string.IsNullOrEmpty(child.To) || !sqlQueryStructures.TryGetValue(child.To, out var childStructure))
                    continue;
                
                var childAlias = childStructure.Alias;
                var childTree  = entityTrees[child.To];
                
                // bubble up all the columns from child to parent
                foreach (var col in childStructure.Columns)
                {
                    if (!parentColumns.Contains(col))
                        parentColumns.Add(col);
                }

                // Also expose the FK column needed by the parent join
                if (!string.IsNullOrEmpty(child.ToColumn))
                {
                    var fkColumn = $"{childAlias}.\"{child.ToColumn}\" AS \"{child.ToColumn}{childTree.Id}\"";

                    if (!parentColumns.Contains(fkColumn))
                        parentColumns.Add(fkColumn);
                }

                // Only propagate if this child actually belongs to the current parent
                bool isRelevantChild = child.From.Matches(currentTree.Alias);
                if (!isRelevantChild) continue;

                foreach (var propagatedColumn in childStructure.ParentColumns)
                {
                    // Only add columns that originate from this child alias
                    if (!propagatedColumn.StartsWith(childAlias + "."))
                        continue;

                    // Avoid duplicates
                    if (!parentColumns.Contains(propagatedColumn))
                        parentColumns.Add(propagatedColumn);
                }
            }

            // ── Build JOIN clauses and propagate columns ─────────────────────
            foreach (var child in allChildren)
            {
                if (string.IsNullOrEmpty(child.To) ||
                    !sqlQueryStructures.TryGetValue(child.To, out var childStructure) ||
                    generatedQueries.Contains(childStructure.Query))
                    continue;

                childStructure.Visited = true;
                generatedQueries.Add(childStructure.Query);

                var childTree  = entityTrees[child.To];
                var childAlias = childStructure.Alias;
                var joinType   = childStructure.SqlNodeType == SqlNodeType.Edge ? "JOIN" : "LEFT JOIN";

                var exportedChildColumn = child.ToColumn.ToSnakeCase(childTree.Id);

                joinClauses.Add(
                    $" {joinType} ( {childStructure.Query} ) {childAlias}" +
                    $" ON {alias}.\"{child.FromColumn}\" = {childAlias}.\"{exportedChildColumn}\"");

                // --- Propagate child columns for parent SELECT ---
                foreach (var col in childStructure.Columns)
                {
                    // Column string looks like: TableName."ColumnName" AS "Alias"
                    // Example: Account."Id" AS "Id______"

                    var parts = col.Split(new[] { " AS " }, StringSplitOptions.None);
                    if (parts.Length != 2)
                        continue; // skip if format is unexpected

                    var originalColumn = parts[0]; // e.g., Account."Id"
                    var aliasSelect = parts[1];           // e.g., "Id______"

                    // Extract column name inside quotes
                    var quoteStart = originalColumn.IndexOf('"');
                    var quoteEnd = originalColumn.LastIndexOf('"');
                    if (quoteStart < 0 || quoteEnd <= quoteStart)
                        continue;

                    // Rebuild column with child alias and correct AS
                    var columnWithAlias = $"{childAlias}.\"{aliasSelect.Trim('"')}\" AS {aliasSelect}";

                    if (!allSelectColumns.Contains(columnWithAlias))
                        allSelectColumns.Add(columnWithAlias);
                }

                // --- Add explicit ParentColumns if needed for further bubbling ---
                foreach (var parentCol in childStructure.ParentColumns)
                {
                    var columnWithAlias = parentCol.Replace("~", childAlias);
                    if (!parentColumns.Contains(columnWithAlias))
                        parentColumns.Add(columnWithAlias);
                }
            }

            var idSnake = "Id".ToSnakeCase(currentTree.Id);

            // ── WHERE clause ────────────────────────────────────────────────
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

            // ── Assemble final query ────────────────────────────────────────
            var select = string.Join(",", allSelectColumns.Distinct());
            var queryBuilder =
                $"SELECT {select} FROM \"{currentTree.Schema}\".\"{currentTree.Name}\" {alias}" +
                string.Join("", joinClauses) +
                whereClause;

            // ── SplitOn registration ───────────────────────────────────────
            // ── SplitOn registration (ORDERED by idSnake length) ─────────────────────
            if (!splitOnDapper.ContainsKey(idSnake))
            {
                var entityType = entityTypes.FirstOrDefault(e => e.Name.Matches(currentTree.Name));

                // 🔥 build temp list (preserves order)
                var splitItems = splitOnDapper.ToList();
                var aliasItems = aliases.ToList();

                var index = splitItems.FindIndex(kvp => kvp.Key.Length > idSnake.Length);

                if (index < 0)
                {
                    splitItems.Add(new KeyValuePair<string, Type>(idSnake, entityType));
                    aliasItems.Add(new KeyValuePair<string, string>(idSnake, currentTree.Alias));
                }
                else
                {
                    splitItems.Insert(index, new KeyValuePair<string, Type>(idSnake, entityType));
                    aliasItems.Insert(index, new KeyValuePair<string, string>(idSnake, currentTree.Alias));
                }
                
                splitOnDapper.Clear();
                aliases.Clear();

                foreach (var item in splitItems)
                    splitOnDapper.Add(item.Key, item.Value);

                foreach (var item in aliasItems)
                    aliases.Add(item.Key, item.Value);
            }

            var currentModelNode = linkModelDictionaryTree.FirstOrDefault(a => a.Key.Contains(currentTree.Name));
            var currentNode      = linkEntityDictionaryTree.FirstOrDefault(a => a.Key.Contains(currentModelNode.Value?.Table));

            if (currentNode.Key == null)
                return;

            var sqlStructure = new SqlQueryStructure
            {
                Id            = currentTree.Id,
                SqlNodeType   = currentNode.Value.SqlNodeTypes.First(),
                SqlNode       = currentNode.Value,
                Query         = queryBuilder,
                Alias         = currentTree.Alias,
                Columns       = allSelectColumns.Distinct().ToList(),
                ParentColumns = parentColumns.Distinct().ToList(),
                LinkKeys      = currentNode.Value?.LinkKeys?.ToList() ?? new List<LinkKey>(),
            };

            if (!sqlQueryStructures.ContainsKey(currentTree.Alias))
                sqlQueryStructures.Add(currentTree.Alias, sqlStructure);
        }

        private static (List<string> ownColumns, List<string> parentColumns,
            List<KeyValuePair<string, SqlNode>> currentColumns)
            GenerateEntityQuery(
                Dictionary<string, NodeTree> entityTrees,
                Dictionary<string, SqlNode> linkEntityDictionaryTree,
                Dictionary<string, SqlNode> sqlStatementNodes,
                NodeTree currentTree,
                Dictionary<string, SqlQueryStructure> sqlQueryStructures,
                Dictionary<string, string> sqlWhereStatement,
                Dictionary<string, string> childrenSqlStatement,
                string rootEntityName,
                List<string> generatedQueries)
        {
            var ownColumns     = new List<string>();
            var parentColumns  = new List<string>();
            var currentColumns = new List<KeyValuePair<string, SqlNode>>();

            // Always include Id
            var idKey = linkEntityDictionaryTree.Keys
                .FirstOrDefault(a => a.Contains($"{currentTree.Alias}~{currentTree.Name}~Id"));

            if (idKey == null)
                return (ownColumns, parentColumns, currentColumns);

            var idNode = linkEntityDictionaryTree[idKey];
            idNode.SqlNodeTypes.Clear();
            idNode.SqlNodeTypes.Add(SqlNodeType.Node);
            currentColumns.Insert(0, new KeyValuePair<string, SqlNode>(idKey, idNode));

            // Add requested fields matching this node's alias or entity name
            currentColumns.AddRange(sqlStatementNodes
                .Where(k =>
                {
                    var parts = k.Key.Split('~');
                    if (parts.Length < 3) return false;
                    return (k.Value.Table.Matches(currentTree.Name))
                           && k.Value.LinkKeys.Any(b => b.From.Matches(parts[0]) &&
                                                        b.FromColumn.Matches(parts[2]));
                }));

            var allChildren = (currentTree.Children ?? new List<LinkKey>())
                .Concat(currentTree.RelatedChildren ?? new List<LinkKey>())
                .ToList();

            var existingSqlQueryStructure =
                sqlQueryStructures.Values.FirstOrDefault(b => b.Alias.Matches(currentTree.Alias));

            if (existingSqlQueryStructure != null && (!allChildren.Any(a => sqlQueryStructures.Values.Any(b => b.Alias.Matches(a.To)))
                                          && currentColumns.Count == 1) ||
                existingSqlQueryStructure?.Visited == true)
                return (ownColumns, parentColumns, currentColumns);

            var idSnake = "Id".ToSnakeCase(currentTree.Id);

            // ── Own SELECT columns ───────────────────────────────────────────
            foreach (var col in currentColumns.DistinctBy(c => c.Key))
            {
                var parts = col.Key.Split('~');
                if (parts.Length < 3) continue;

                var fieldName  = parts[2];
                var snakeField = fieldName.ToSnakeCase(currentTree.Id);

                ownColumns.Add($"{currentTree.Alias}.\"{fieldName}\" AS \"{snakeField}\"");
            }

            // Export FK columns needed by parent joins
            foreach (var parent in (currentTree.Parents ?? new List<LinkKey>())
                     .Concat(currentTree.RelatedParents ?? new List<LinkKey>()))
            {
                if (string.IsNullOrEmpty(parent.FromColumn))
                    continue;

                var fkAlias = parent.FromColumn.ToSnakeCase(currentTree.Id);

                var fkColumn =
                    $"{currentTree.Alias}.\"{parent.FromColumn}\" AS \"{fkAlias}\"";

                if (!ownColumns.Contains(fkColumn))
                    ownColumns.Add(fkColumn);

                if (!parentColumns.Contains($"~.\"{fkAlias}\" AS \"{fkAlias}\""))
                    parentColumns.Add($"~.\"{fkAlias}\" AS \"{fkAlias}\"");
            }

            // ── Parent columns: always expose Id ─────────────────────────────
            parentColumns.Add($"~.\"{idSnake}\" AS \"{idSnake}\"");

            foreach (var col in currentColumns.DistinctBy(c => c.Key).Skip(1))
            {
                var parts = col.Key.Split('~');
                if (parts.Length < 3) continue;

                var snakeField = parts[2].ToSnakeCase(currentTree.Id);
                parentColumns.Add($"~.\"{snakeField}\" AS \"{snakeField}\"");
            }

            return (ownColumns, parentColumns, currentColumns);
        }
    }
}