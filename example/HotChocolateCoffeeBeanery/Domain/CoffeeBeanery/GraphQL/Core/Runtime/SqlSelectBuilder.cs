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
        public static (string, OrderedDictionary<string, Type>, List<string>) Build(
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

            foreach (var edgeKey in edgeDict)
            {
                if (!nodeDict.ContainsKey(edgeKey.Key))
                    nodeDict.Add(edgeKey.Key, edgeKey.Value);
            }

            GenerateQuery(
                SqlNodeRegistry.EntityTrees,
                SqlNodeRegistry.EntityTypes,
                SqlNodeRegistry.EntityNodes,
                SqlNodeRegistry.ModelNodes,
                nodeDict,
                sqlWhereStatement,
                SqlNodeRegistry.EntityTrees[wrapperEntityName],
                childrenSqlStatement,
                wrapperEntityName,
                sqlQueryStructures,
                splitOnDapper,
                aliases,
                entityOrder,
                new List<string>(),
                new List<string>(),
                new List<string>());

            if (sqlQueryStructures.Count == 0)
                return (string.Empty, default, default);

            var aliasesOrdered = new List<string>();
            foreach (var alias in aliases.Reverse())
                aliasesOrdered.Add(alias.Value);

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
            var alias = string.IsNullOrEmpty(currentTree.Alias)
                ? currentTree.Name
                : currentTree.Alias;

            if (parentVisitedEntities.Contains(alias) ||
                sqlQueryStructures.Any(a => a.Value.Alias == currentTree.Alias))
                return;

            parentVisitedEntities.Add(alias);
            currentTree = entityTrees[alias];
            entityOrder.Add(currentTree.Name);

            var allChildren = (currentTree.Children ?? new List<LinkKey>())
                .Concat(currentTree.RelatedChildren ?? new List<LinkKey>())
                .ToList();

            // ── POST-ORDER: recurse into all children first ───────────────────
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

                    childVisitedEntities = new List<string>(parentVisitedEntities) { currentTree.Name };

                    if (currentTree.Parents?.Count > 0 && !string.IsNullOrEmpty(currentTree.Parents[0].To))
                        childVisitedEntities.Add(currentTree.Parents[0].To);

                    if (currentTree.RelatedParents?.Count > 0 && !string.IsNullOrEmpty(currentTree.RelatedParents[0].To))
                        childVisitedEntities.Add(currentTree.RelatedParents[0].To);

                    GenerateQuery(
                        entityTrees, entityTypes,
                        linkEntityDictionaryTree, linkModelDictionaryTree,
                        sqlStatementNodes, sqlWhereStatement,
                        entityTrees[child.To],
                        childrenSqlStatement, rootEntityName,
                        sqlQueryStructures, splitOnDapper, aliases,
                        entityOrder, parentVisitedEntities,
                        childVisitedEntities, generatedQueries);
                }
            }

            // ── Generate columns for current node ─────────────────────────────
            var (ownColumns, parentQueryColumns, currentColumns) = GenerateEntityQuery(
                entityTrees, linkEntityDictionaryTree, sqlStatementNodes,
                currentTree, sqlQueryStructures, sqlWhereStatement,
                childrenSqlStatement, rootEntityName, generatedQueries);

            if (ownColumns.Count == 0)
                return;

            // ── Build SELECT list starting with own columns ───────────────────
            var allSelectColumns = new List<string>(ownColumns);

            // ── JOIN children ─────────────────────────────────────────────────
            var joinClauses = new List<string>();

            foreach (var child in allChildren)
            {
                if (string.IsNullOrEmpty(child.To) ||
                    !sqlQueryStructures.ContainsKey(child.To) ||
                    generatedQueries.Contains(sqlQueryStructures[child.To].Query))
                    continue;

                var childStructure = sqlQueryStructures[child.To];
                sqlQueryStructures[child.To].Visited = true;
                generatedQueries.Add(childStructure.Query);

                var childTree  = entityTrees[child.To];
                var childAlias = childStructure.Alias;
                var joinType   = childStructure.SqlNodeType == SqlNodeType.Edge ? "JOIN" : "LEFT JOIN";

                // JOIN ON:
                // parent side: parent alias . FromColumn  (e.g. CustomerCustomerRelationship."InnerCustomerId")
                // child side:  child alias  . "Id" snake-cased at child's own ID level
                //              (e.g. InnerCustomer."Id____" where InnerCustomer.Id = 4 underscores)
                var childIdSnake = "Id".ToSnakeCase(childTree.Id);

                joinClauses.Add(
                    $" {joinType} ( {childStructure.Query} ) {childAlias}" +
                    $" ON {alias}.\"{child.FromColumn}\" = {childAlias}.\"{childIdSnake}\"");

                // Pull child ParentColumns into this SELECT — replace ~ placeholder with child alias
                allSelectColumns.AddRange(
                    childStructure.ParentColumns
                        .Select(s => s.Replace("~", childAlias))
                        .Where(s => !allSelectColumns.Contains(s)));
            }

            // ── WHERE clause ──────────────────────────────────────────────────
            var whereClause = string.Empty;
            sqlWhereStatement.TryGetValue(currentTree.Name, out var whereStatement);
            if (!string.IsNullOrEmpty(whereStatement))
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

            // ── Assemble final query ──────────────────────────────────────────
            var select      = string.Join(",", allSelectColumns.Distinct());
            var queryBuilder =
                $"SELECT {select}" +
                $" FROM \"{currentTree.Schema}\".\"{currentTree.Name}\" {alias}" +
                string.Join("", joinClauses) +
                whereClause;

            // ── SplitOn registration ──────────────────────────────────────────
            if (!splitOnDapper.ContainsKey("Id".ToSnakeCase(currentTree.Id)))
            {
                var entityType = entityTypes.FirstOrDefault(e => e.Name.Matches(currentTree.Name));
                if (splitOnDapper.Values.Any(a => a.Name.Matches(currentTree.Name)))
                {
                    splitOnDapper.Insert(0, "Id".ToSnakeCase(currentTree.Id), entityType);
                    aliases.Insert(0, "Id".ToSnakeCase(currentTree.Id), currentTree.Alias);
                }
                else
                {
                    splitOnDapper.Add("Id".ToSnakeCase(currentTree.Id), entityType);
                    aliases.Add("Id".ToSnakeCase(currentTree.Id), currentTree.Alias);
                }
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
                ParentColumns = parentQueryColumns.Distinct().ToList(),
            };

            if (!sqlQueryStructures.TryGetValue(currentTree.Alias, out _))
                sqlQueryStructures.Add(currentTree.Alias, sqlStructure);
        }

        /// <summary>
        /// Returns own SELECT columns, parent propagation columns, and raw column pairs.
        /// Key format: "{model}~{entity}~{field}" → [0]=model [1]=entity [2]=field
        ///
        /// Own columns:    what this node SELECTs from its own table
        /// Parent columns: what this node exposes upward (with ~ as alias placeholder)
        ///                 Always includes "Id" at this node's snake level so parent
        ///                 can JOIN ON child."Id__" = parent."FKColumn"
        /// </summary>
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
                    return (parts[0].Matches(currentTree.Alias) || parts[1].Matches(currentTree.Name))
                           && !k.Value.LinkKeys.Any(b => b.From.Matches(k.Key));
                }));

            var allChildren = (currentTree.Children ?? new List<LinkKey>())
                .Concat(currentTree.RelatedChildren ?? new List<LinkKey>())
                .ToList();

            var existingSqlQueryStructure =
                sqlQueryStructures.Values.FirstOrDefault(b => b.Alias.Matches(currentTree.Alias));

            if ((!allChildren.Any(a => sqlQueryStructures.Values.Any(b => b.Alias.Matches(a.To)))
                 && currentColumns.Count == 1) ||
                existingSqlQueryStructure?.Visited == true)
                return (ownColumns, parentColumns, currentColumns);

            var idSnake = "Id".ToSnakeCase(currentTree.Id);

            // ── Own SELECT columns ────────────────────────────────────────────
            foreach (var col in currentColumns.DistinctBy(c => c.Key))
            {
                var parts = col.Key.Split('~');
                if (parts.Length < 3) continue;

                var fieldName  = parts[2];
                var snakeField = fieldName.ToSnakeCase(currentTree.Id);

                ownColumns.Add($"{currentTree.Alias}.\"{fieldName}\" AS \"{snakeField}\"");
            }

            // ── Parent columns: expose Id at this node's snake level ──────────
            // Parent JOINs ON child."Id__" so we must expose it in ParentColumns
            // as "~."Id__" AS "Id__"" (~ is replaced with child alias by parent)
            parentColumns.Add($"~.\"{idSnake}\" AS \"{idSnake}\"");

            // Also propagate any requested field columns upward
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