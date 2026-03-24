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
        public static SqlStructure Compile(
            ISelection rootSelection,
            NodeTree rootTree,
            string wrapperEntityName,
            Dictionary<string, SqlNode> mutationDict,
            Dictionary<string, string> sqlWhereStatement)
        {
            var ctx = new SqlCompilationContext();
            var generatedQuery = new List<string>();
            var sqlUpsertBuilder  = new StringBuilder();
            var sqlSelectUpsertBuilder = new StringBuilder();

            if (sqlWhereStatement.Count == 0)
            {
                SqlWhereCompiler.Compile(ctx, rootSelection, rootTree, wrapperEntityName, sqlWhereStatement);    
            }
            
            GenerateUpsertStatements(SqlNodeRegistry.ModelTrees, SqlNodeRegistry.EntityTrees, SqlNodeRegistry.EntityNodes,
                wrapperEntityName, generatedQuery, mutationDict, SqlNodeRegistry.EntityTrees[rootTree.Name],
                sqlWhereStatement, new List<string>(),
                sqlUpsertBuilder, sqlSelectUpsertBuilder);
            
            ctx.UpsertSql = sqlUpsertBuilder.ToString() + " ; " + sqlSelectUpsertBuilder.ToString();

            return new SqlStructure
            {
                SqlUpsert = ctx.UpsertSql
            };
        }

        public static string GenerateUpsertStatements(Dictionary<string, NodeTree> modeltrees, Dictionary<string, NodeTree> entitytrees,
            Dictionary<string, SqlNode> sqlNodes, string wrapperEntityName,
            List<string> generatedQuery, Dictionary<string, SqlNode> sqlUpsertStatementNodes, NodeTree currentTree,
            Dictionary<string, string> sqlWhereStatement, List<string> entitiesProcessed,
            StringBuilder sqlUpsertBuilder, StringBuilder sqlSelectUpsertBuilder)
        {
            var sqlUpsert = string.Empty;
            var entityNames = sqlNodes.Keys.ToList();
            var rootEntityName = currentTree.Name;

            if (entitiesProcessed.Contains(rootEntityName))
            {
                return string.Empty;
            }

            entitiesProcessed.Add(rootEntityName);

            var processingTree = currentTree;
            var whereParentValue = sqlWhereStatement.GetValueOrDefault(processingTree.ParentName);
            var whereParentClause = string.Empty;
            if (!string.IsNullOrEmpty(whereParentValue))
            {
                whereParentClause = $" WHERE {whereParentValue.Replace("~", processingTree.ParentName)}";
            }

            var whereCurrentValue = sqlWhereStatement.GetValueOrDefault(processingTree.Name);
            var whereCurrentClause = string.Empty;

            if (!string.IsNullOrEmpty(whereCurrentClause) && string.IsNullOrEmpty(whereParentClause))
            {
                whereCurrentClause = $" WHERE {whereCurrentValue.Replace("~", processingTree.Name)}";
            }

            if (!string.IsNullOrEmpty(whereCurrentClause) && !string.IsNullOrEmpty(whereParentClause))
            {
                whereCurrentClause += $" {whereParentClause} {whereCurrentValue.Replace("~", processingTree.Name)}";
            }

            var upsertingEntity = sqlUpsertStatementNodes.FirstOrDefault(s =>
                s.Key.Split('~')[0].Matches(processingTree.Name) || !s.Value.LinkKeys
                    .Any(a => a.To.Split('~')[0].Matches(processingTree.ParentName)));

            var sql = string.Empty;

            if (upsertingEntity.Value != null)
            {
                sql = GenerateUpsert(processingTree, entitytrees, sqlUpsertStatementNodes, whereCurrentClause, entityNames, sqlNodes);

                if (!string.IsNullOrEmpty(sql))
                {
                    generatedQuery.Add(sql);
                    sqlUpsertBuilder.Append(generatedQuery.Last());
                    sqlUpsertBuilder.Insert(0, " ; " + sql);
                    sqlSelectUpsertBuilder.Insert(0, " ; " + GenerateSelectUpsert(processingTree, sqlNodes, entityNames,
                        entitytrees, sqlUpsertStatementNodes, sqlWhereStatement, new List<string>(), rootEntityName,
                        generatedQuery,
                        wrapperEntityName));
                }
            }

            foreach (var childTreeName in processingTree.Children)
            {
                var childTree = entitytrees[childTreeName];
                var childVisitedEntities = new List<string>(entitiesProcessed)
                {
                    currentTree.Name
                };

                if (!string.IsNullOrEmpty(currentTree.ParentName))
                {
                    childVisitedEntities.Add(currentTree.ParentName);
                }
                
                GenerateUpsertStatements(modeltrees, entitytrees, sqlNodes, wrapperEntityName, generatedQuery,
                    sqlUpsertStatementNodes, childTree, sqlWhereStatement, childVisitedEntities,
                    sqlUpsertBuilder, sqlSelectUpsertBuilder);
            }

            return sqlUpsert;
        }

        /// <summary>
        /// Generate the main upsert without "Join columns [Ids]"
        /// </summary>
        /// <param name="currentTree"></param>
        /// <param name="trees"></param>
        /// <param name="sqlUpsertStatementNodes"></param>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        public static string GenerateUpsert(NodeTree currentTree, Dictionary<string, NodeTree> trees,
            Dictionary<string, SqlNode> sqlUpsertStatementNodes,
            string whereClause, List<string> entityNames, Dictionary<string, SqlNode> sqlNodes)
        {
            var sqlUpsertAux = string.Empty;
            
            var currentColumns = new List<KeyValuePair<string, SqlNode>>();
            var aux = sqlNodes
                .FirstOrDefault(k => k.Value.Table.Matches(currentTree.Name));

            foreach (var upsertKey in aux.Value.UpsertKeys)
            {
                currentColumns.Add(new KeyValuePair<string, SqlNode>(upsertKey, aux.Value));
            }

            currentColumns.AddRange(sqlUpsertStatementNodes
                .Where(k => !k.Value.Value.Matches("") && k.Key.Split('~')[0].Matches($"{currentTree.Name}")).ToList());

            var node = sqlNodes
                .FirstOrDefault(k => k.Key.Contains($"{currentTree.Alias}~"));
            
            if (node.Value == null || currentColumns.Count <= node.Value.UpsertKeys.Count() || node.Value.UpsertKeys?.Count() == 0)
            {
                return string.Empty;
            }
            
            currentColumns = currentColumns.Where(a => !string.IsNullOrEmpty(a.Value.Value)).DistinctBy(a => a.Key).ToList();

            sqlUpsertAux += $" INSERT INTO \"{currentTree.Schema}\".\"{currentTree.Name}\" ( " +
                            $" {string.Join(",", currentColumns.Select(s => $"\"{s.Key.Split('~')[0]}\"").ToList())}) VALUES ({
                                string.Join(",", currentColumns.Select(s => $"'{s.Value.Value}'").ToList())}) " +
                            $" ON CONFLICT" +
                            $" ({string.Join(",", currentColumns.FirstOrDefault(a => a.Value.UpsertKeys
                                    .Any(u => currentColumns.Any(c => u.Matches(c.Key)))).Value.UpsertKeys
                                .Where(u => currentColumns.Any(c => u.Matches(c.Key)))
                                .Select(s => $"\"{s.Split('~')[1]}\"").ToList())}) ";

            var exclude = new List<string>();
            exclude.AddRange(
                currentColumns.Where(c => c.Value.UpsertKeys
                        .Any(u => !u.Matches(c.Value.RelationshipKey.Split('~')[1])))
                    .Select(e =>
                        $"\"{e.Value.RelationshipKey.Split('~')[2]}\" = EXCLUDED.\"{e.Value.RelationshipKey.Split('~')[2]}\"")
            );

            if (exclude.Count > 0)
            {
                sqlUpsertAux += $" DO UPDATE SET {string.Join(",", exclude)} {whereClause};";
            }
            else
            {
                sqlUpsertAux += $" DO NOTHING {whereClause};";
            }

            return sqlUpsertAux;
        }

        /// <summary>
        /// Generate the upsert for "Join columns [Ids]"
        /// </summary>
        /// <param name="currentTree"></param>
        /// <param name="entityNames"></param>
        /// <param name="trees"></param>
        /// <param name="sqlUpsertStatementNodes"></param>
        /// <param name="whereClause"></param>
        /// <returns></returns>
        public static string GenerateSelectUpsert(NodeTree currentTree, Dictionary<string, SqlNode> sqlNodes,
            List<string> entityNames,
            Dictionary<string, NodeTree> trees,
            Dictionary<string, SqlNode> sqlUpsertStatementNodes,
            Dictionary<string, string> sqlWhereStatement, List<string> entitiesProcessed, string rootEntityName,
            List<string> generatedQuery, string wrapperEntityName)
        {
            if (entitiesProcessed.Contains(currentTree.Name))
            {
                return string.Empty;
            }

            entitiesProcessed.Add(currentTree.Name);

            var sqlUpsertAux = string.Empty;
            var hasUpsert = true;

            var currentColumns = sqlUpsertStatementNodes
                .Where(k => currentTree.Mapping.Any(f => f.DestinationName.Matches(k.Key.Split('~')[1]) &&
                                                         !entityNames.Contains(k.Key.Split('~')[0]))).ToList();

            if (currentColumns.Count == 0)
            {
                return sqlUpsertAux;
            }

            var columnsQuery = currentColumns.Where(c => c.Value.UpsertKeys.Any(k =>
                k.Matches(c.Value.RelationshipKey))).ToList();

            var columnValue = columnsQuery.FirstOrDefault(a => a.Key.Split('~')[0]
                .Matches(currentTree.Name)).Value;

            if (columnValue == null)
            {
                return sqlUpsertAux;
            }

            foreach (var joinKey in columnValue.LinkKeys)
            {
                if (!joinKey.To.Split('~')[0].Matches(currentTree.Name))
                {
                    var columns = columnsQuery.ToList();
                    columns.Add(new KeyValuePair<string, SqlNode>(joinKey.To, currentColumns.Last().Value));

                    var parentColumns = sqlUpsertStatementNodes
                        .Where(k => trees[joinKey.To.Split('~')[0]].Mapping.Any(f => f
                                .DestinationName.Matches(k.Key.Split('~')[1]) &&
                            !entityNames.Any(e => e.Matches(k.Key.Split('~')[0])))).ToList();

                    sqlUpsertAux += GenerateCommand(columns, trees, currentTree, sqlWhereStatement, parentColumns,
                        entityNames, joinKey.To.Split('~')[0]);
                }
            }

            // foreach (var joinOneKey in columnValue.JoinOneKeys)
            // {
            //     if (!joinOneKey.From.Split('~')[0].Matches(currentTree.Name))
            //     {
            //         var columns = columnsQuery.ToList();
            //         columns.Add(new KeyValuePair<string, SqlNode>(joinOneKey.From, currentColumns.Last().Value));
            //
            //         var parentColumns = sqlUpsertStatementNodes
            //             .Where(k => trees[joinOneKey.From.Split('~')[0]]
            //                 .Mapping.Any(f => f
            //                                       .FieldDestinationName.Matches(k.Key.Split('~')[1]) &&
            //                                   !entityNames.Any(e => e.Matches(k.Key.Split('~')[1])))).ToList();
            //
            //         sqlUpsertAux += GenerateCommand(columns, trees, currentTree, sqlWhereStatement, parentColumns,
            //             entityNames, joinOneKey.From.Split('~')[0]);
            //     }
            // }

            return sqlUpsertAux;
        }

        private static string GenerateCommand(List<KeyValuePair<string, SqlNode>> currentColumns,
            Dictionary<string, NodeTree> trees,
            NodeTree currentTree, Dictionary<string, string> sqlWhereStatement,
            List<KeyValuePair<string, SqlNode>> parentColumns,
            List<string> entityNames, string entity)
        {
            currentColumns = currentColumns.DistinctBy(a => a.Key).ToList();

            if (string.IsNullOrEmpty(entity))
            {
                return string.Empty;
            }

            var parentTree = trees[entity];

            var insertJoin = $"\"{entity}Id\"";
            var selectJoin = $"{entity}.\"Id\" AS" +
                             $" \"{entity}Id\"";

            var onConflictKey = currentColumns.FirstOrDefault(a => a.Value
                                                                       .UpsertKeys
                                                                       .Any(x => x == a.Value.RelationshipKey) &&
                                                                   a.Value.RelationshipKey.Split('~')[0]
                                                                       .Matches(currentTree.Name));

            insertJoin += $", \"{onConflictKey.Value.Column}\"";
            selectJoin += $", '{onConflictKey.Value.Value}' AS \"{onConflictKey.Value.Column}\"";

            var excludeJoin = $"\"{entity}Id\" = EXCLUDED.\"{entity}Id\"";

            var where = parentColumns.Where(a =>
                    a.Key.Split('~')[0].Matches(entity) &&
                    a.Value.UpsertKeys.Any(k => k.Matches(a.Value.RelationshipKey)))
                .Select(s => $"{entity}.\"{s.Value.Column}\" = '{s.Value.Value}'").ToList();

            if (
                currentColumns.Count == 0 ||
                !currentColumns.Any(a => a.Value.UpsertKeys
                                             .Any(u => u.Split('~')[1].Matches(a.Value.Column)) &&
                                         a.Value.SqlNodeTypes.Contains(SqlNodeType.Mutation) &&
                                         !string.IsNullOrEmpty(a.Value.Value)) ||
                where.Count == 0)
            {
                return string.Empty;
            }

            var sqlUpsertAux = $" ; INSERT INTO \"{currentTree.Schema}\".\"{currentTree.Name}\" ( " +
                               insertJoin +
                               $" ) ( SELECT {selectJoin}" +
                               $" FROM \"{parentTree.Schema}\".\"{entity}\" {entity} WHERE {
                                   string.Join(" AND ", where)}";

            var exclude = new List<string>();
            exclude.Add(excludeJoin);

            sqlUpsertAux += $" ) ON CONFLICT" +
                            $" (\"{onConflictKey.Value.Column}\") ";

            if (exclude.Count > 0)
            {
                sqlUpsertAux += $" DO UPDATE SET {string.Join(",", exclude)}";
            }
            else
            {
                sqlUpsertAux += $" DO NOTHING";
            }

            return sqlUpsertAux;
        }
    }
}