using System.Collections.Generic;
using System.Text;
using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public class SqlMutationCompiler
    {
        // Key format:             "{alias}~{field}"          → [0]=alias  [1]=field
        // RelationshipKey format: "{model}~{entity}~{field}" → [0]=model  [1]=entity  [2]=field
        // UpsertKey format:       "{alias}~{entity}~{field}" → [0]=alias  [1]=entity  [2]=field

        public static SqlStructure Compile(
            ISelection rootSelection,
            NodeTree rootTree,
            string wrapperEntityName,
            Dictionary<string, SqlNode> mutationDict,
            Dictionary<string, string> sqlWhereStatement)
        {
            var ctx                    = new SqlCompilationContext();
            var generatedQuery         = new List<string>();
            var sqlUpsertBuilder       = new StringBuilder();
            var sqlSelectUpsertBuilder = new StringBuilder();

            if (sqlWhereStatement.Count == 0)
                SqlWhereCompiler.Compile(ctx, rootSelection, rootTree, wrapperEntityName, sqlWhereStatement);

            var entitiesProcessed = new List<string>();
            
            var nodeTreeKeyValuePair =
                SqlNodeRegistry.ModelTrees.FirstOrDefault(a => a.Value.ModelToEntityLinks[0].AliasTo.Matches(wrapperEntityName));

            if (nodeTreeKeyValuePair.Value == null)
            {
                nodeTreeKeyValuePair = SqlNodeRegistry.ModelTrees.FirstOrDefault(a => a.Value.ModelToEntityLinks[0].AliasFrom.Matches(wrapperEntityName));
            }

            foreach (var fieldMap in nodeTreeKeyValuePair.Value.Mapping)
            {
                GenerateUpsertStatements(
                    SqlNodeRegistry.EntityTrees,
                    SqlNodeRegistry.EntityNodes,
                    wrapperEntityName,
                    generatedQuery,
                    mutationDict,
                    SqlNodeRegistry.EntityTrees.First(a => a.Value.Name.Matches(fieldMap.DestinationEntity)).Value,
                    sqlWhereStatement,
                    entitiesProcessed,
                    sqlUpsertBuilder,
                    sqlSelectUpsertBuilder);    
            }

            ctx.UpsertSql = sqlUpsertBuilder.ToString() + " ; " + sqlSelectUpsertBuilder.ToString();

            return new SqlStructure { SqlUpsert = ctx.UpsertSql };
        }

        public static string GenerateUpsertStatements(
            Dictionary<string, NodeTree> entityTrees,
            Dictionary<string, SqlNode> sqlNodes,
            string wrapperEntityName,
            List<string> generatedQuery,
            Dictionary<string, SqlNode> sqlUpsertStatementNodes,
            NodeTree currentTree,
            Dictionary<string, string> sqlWhereStatement,
            List<string> entitiesProcessed,
            StringBuilder sqlUpsertBuilder,
            StringBuilder sqlSelectUpsertBuilder)
        {
            var currentAlias = currentTree.Alias ?? currentTree.Name;

            if (entitiesProcessed.Contains(currentAlias))
                return string.Empty;

            entitiesProcessed.Add(currentAlias);

            // Combine Children + RelatedChildren
            var allChildren = new List<LinkKey>();
            if (currentTree.Children != null)
                allChildren.AddRange(currentTree.Children);
            if (currentTree.RelatedChildren != null)
                allChildren.AddRange(currentTree.RelatedChildren);

            // ── POST-ORDER: recurse into ALL children first ───────────────────
            // For each child link, find ALL aliased variants sharing the same
            // entity Name (e.g. both InnerCustomer and OuterCustomer for "Customer")
            // and recurse into each one that hasn't been processed yet
            foreach (var childLink in allChildren)
            {
                if (!entityTrees.TryGetValue(childLink.To, out var childTree))
                    continue;

                // Find all aliased variants of this child entity
                var childVariants = entityTrees
                    .Where(t => t.Value.Name == childTree.Name &&
                                !entitiesProcessed.Contains(t.Key))
                    .ToList();

                // Always include the direct child itself
                if (!childVariants.Any(v => v.Key == childLink.To))
                    childVariants.Insert(0, new KeyValuePair<string, NodeTree>(childLink.To, childTree));

                foreach (var (variantAlias, variantTree) in childVariants)
                {
                    if (entitiesProcessed.Contains(variantAlias))
                        continue;

                    GenerateUpsertStatements(
                        entityTrees, sqlNodes, wrapperEntityName,
                        generatedQuery, sqlUpsertStatementNodes, variantTree,
                        sqlWhereStatement, entitiesProcessed,
                        sqlUpsertBuilder, sqlSelectUpsertBuilder);
                }
            }

            // ── NOW upsert current node — all children already persisted ──────
            var hasData = sqlUpsertStatementNodes
                .Any(s => s.Key.Split('~')[0].Matches(currentAlias) &&
                          !string.IsNullOrEmpty(s.Value.Value));

            if (!hasData)
                return string.Empty;

            var whereParentValue = currentTree.Parents?.Count > 0
                ? sqlWhereStatement.GetValueOrDefault(currentTree.Parents[0].To)
                : string.Empty;

            var whereParentClause = string.Empty;
            if (!string.IsNullOrEmpty(whereParentValue))
                whereParentClause = $" WHERE {whereParentValue.Replace("~", currentTree.Parents![0].To)}";

            var whereCurrentValue  = sqlWhereStatement.GetValueOrDefault(currentAlias);
            var whereCurrentClause = string.Empty;

            if (!string.IsNullOrEmpty(whereCurrentValue) && string.IsNullOrEmpty(whereParentClause))
                whereCurrentClause = $" WHERE {whereCurrentValue.Replace("~", currentAlias)}";

            if (!string.IsNullOrEmpty(whereCurrentValue) && !string.IsNullOrEmpty(whereParentClause))
                whereCurrentClause += $" {whereParentClause} {whereCurrentValue.Replace("~", currentAlias)}";

            var sql = GenerateUpsert(
                currentTree, currentAlias, entityTrees,
                sqlUpsertStatementNodes, whereCurrentClause, sqlNodes);

            if (!string.IsNullOrEmpty(sql) && !generatedQuery.Contains(sql))
            {
                generatedQuery.Add(sql);
                sqlUpsertBuilder.Insert(0, " ; " + sql);
                sqlSelectUpsertBuilder.Insert(0, " ; " + GenerateSelectUpsert(
                    currentTree, currentAlias, sqlNodes,
                    entityTrees, sqlUpsertStatementNodes,
                    sqlWhereStatement, new List<string>(),
                    generatedQuery, wrapperEntityName));
            }

            return string.Empty;
        }

        public static string GenerateUpsert(
            NodeTree currentTree,
            string currentAlias,
            Dictionary<string, NodeTree> trees,
            Dictionary<string, SqlNode> sqlUpsertStatementNodes,
            string whereClause,
            Dictionary<string, SqlNode> sqlNodes)
        {
            var sqlUpsertAux = string.Empty;

            var currentColumns = sqlUpsertStatementNodes
                .Where(k =>
                    k.Key.Split('~')[0].Matches(currentAlias) &&
                    !string.IsNullOrEmpty(k.Value.Value)
                    )
                .ToList();

            if (currentColumns.Count == 0)
                return sqlUpsertAux;

            currentColumns = currentColumns.DistinctBy(a => a.Key).ToList();

            var upsertKeyMatch = currentColumns.FirstOrDefault(a =>
                a.Value.UpsertKeys.Any(u =>
                    currentColumns.Any(c =>
                        u.Split('~').Last().Matches(c.Key.Split('~')[2]))));

            if (upsertKeyMatch.Value == null)
                return sqlUpsertAux;

            sqlUpsertAux +=
                $" INSERT INTO \"{currentTree.Schema}\".\"{currentTree.Name}\" ( " +
                $"{string.Join(",", currentColumns.Select(s => $"\"{s.Value.Column}\""))}" +
                $") VALUES ({string.Join(",", currentColumns.Select(s => $"'{s.Value.Value}'"))})" +
                $" ON CONFLICT ({string.Join(",", currentColumns.Where(a =>
                        a.Value.UpsertKeys.Any(u =>
                            u.Split('~').Last().Matches(a.Key.Split('~')[2])))
                    .Select(s => $"\"{s.Value.RelationshipKey.Split('~').Last()}\""))}) ";
            
            var exclude = currentColumns
                .Where(c => c.Value.UpsertKeys.Any(u =>
                    !u.Split('~').Last().Matches(c.Value.Column)))
                .Select(e =>
                    $"\"{e.Value.Column}\" = EXCLUDED.\"{e.Value.Column}\"")
                .ToList();

            sqlUpsertAux += exclude.Count > 0
                ? $" DO UPDATE SET {string.Join(",", exclude)} {whereClause};"
                : $" DO NOTHING {whereClause};";

            return sqlUpsertAux;
        }

        public static string GenerateSelectUpsert(
            NodeTree currentTree,
            string currentAlias,
            Dictionary<string, SqlNode> sqlNodes,
            Dictionary<string, NodeTree> trees,
            Dictionary<string, SqlNode> sqlUpsertStatementNodes,
            Dictionary<string, string> sqlWhereStatement,
            List<string> entitiesProcessed,
            List<string> generatedQuery,
            string wrapperEntityName)
        {
            
            
            if (entitiesProcessed.Contains(currentAlias))
                return string.Empty;

            entitiesProcessed.Add(currentAlias);

            var sqlUpsertAux = string.Empty;

            var currentColumns = sqlUpsertStatementNodes
                .Where(k =>
                    k.Key.Split('~')[0].Matches(currentAlias) &&
                    !string.IsNullOrEmpty(k.Value.Value))
                .ToList();
            //
            // if (currentColumns.Count == 0)
            //     return sqlUpsertAux;

            // var columnsQuery = currentColumns
            //     .Where(c => c.Value.UpsertKeys.Any(u =>
            //         u.Split('~').Last().Matches(c.Key.Split('~')[0])))
            //     .ToList();
            //
            // var columnValue = columnsQuery
            //     .FirstOrDefault(a => a.Key.Split('~')[0].Matches(currentAlias)).Value;
            //
            // if (columnValue == null)
            //     return sqlUpsertAux;

            foreach (var parent in currentTree.Parents)
            {
                    // var linkedCustomerColumn = sqlUpsertStatementNodes
                    //     .FirstOrDefault(k =>
                    //         k.Key.Split('~')[0].Matches(linkKey.To) &&
                    //         k.Key.Split('~')[1].Matches(linkKey.ToColumn));
                    //
                    // if (!string.IsNullOrEmpty(linkedCustomerColumn.Key))
                    // {
                    //     columns.Add(linkedCustomerColumn);
                    // }
                    //
                    var parentColumns = sqlUpsertStatementNodes
                        .Where(k =>
                            k.Key.Split('~')[0].Matches(parent.To) &&
                            !string.IsNullOrEmpty(k.Value.Value))
                        .ToList();

                    var childColumns = sqlUpsertStatementNodes.Where(a => currentColumns
                            .Any(b => b.Key.Split('~')[2].Matches(a.Key.Split('~')[2]) &&
                                      !a.Value.EntityParents.Any(a => a.ToColumn.Matches(b.Value.Column)))).ToList();
                    
                    sqlUpsertAux += GenerateCommand(
                        currentColumns, sqlUpsertStatementNodes, trees, currentTree,
                        sqlWhereStatement, childColumns, parent.To);
                    
            }

            return sqlUpsertAux;
        }

        private static string GenerateCommand(
            List<KeyValuePair<string, SqlNode>> currentColumns,
            Dictionary<string, SqlNode> sqlUpsertStatementNodes,
            Dictionary<string, NodeTree> trees,
            NodeTree currentTree,
            Dictionary<string, string> sqlWhereStatement,
            List<KeyValuePair<string, SqlNode>> childrenColumns,
            string linkedAlias)
        {
            if (string.IsNullOrEmpty(linkedAlias) || !trees.TryGetValue(linkedAlias, out var parentTree))
                return string.Empty;
            
            var sqlUpsert = string.Empty;
            var currentParentTree = trees[linkedAlias];

            // foreach (var childColumn in childrenColumns)
            // {
                var entityLink = currentParentTree.ModelToEntityLinks.FirstOrDefault(a => a.From.Matches(currentTree.Alias));
                // if (entityLink == null || !trees.TryGetValue(entityLink.From, out var entityTree))
                // {
                //     continue;
                // }

                var childColumn = childrenColumns.FirstOrDefault(a =>
                    a.Key.Split('~')[0].Matches(entityLink.From) &&
                    a.Value.UpsertKeys.Any(b => b.Split('~')[1].Matches(a.Value.Column)));

                var sqlUpsertAux =
                    $" ; UPDATE \"{parentTree.Schema}\".\"{parentTree.Name}\" SET \"{currentTree.Alias}Id\" = ( SELECT {
                        currentTree.Alias}.\"Id\" AS \"{currentTree.Alias}Id\"" +
                    $" FROM \"{currentTree.Schema}\".\"{currentTree.Name}\" {currentTree.Alias}" +
                    $" WHERE {currentTree.Alias}.\"{entityLink.ToColumn}\" = '{childColumn.Value.Value}') WHERE \"{
                        entityLink.FromColumn}\" = '{childColumn.Value.Value}'";

                sqlUpsert += sqlUpsertAux + " ; ";
            // }
            
            return sqlUpsert;
        }
    }
}