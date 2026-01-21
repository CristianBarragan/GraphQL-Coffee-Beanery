using System;
using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.Helper;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using FASTER.core;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public static class SqlNodeResolverHelper
    {
        public static SqlStructure HandleGraphQL<D, S>(
            ISelection graphQlSelection,
            IEntityTreeMap<D, S> entityTreeMap,
            IModelTreeMap<D, S> modelTreeMap,
            string rootModelName,
            string wrapperEntityName,
            IFasterKV<string, string> cache,
            string cacheKey,
            Dictionary<string, List<string>> permissions = null)
            where D : class
            where S : class
        {
            var ctx = new SqlCompilationContext();
            var models = modelTreeMap.ModelNames;
            var transformedToParent = false;
            var rootEntityName = rootModelName;

            while (!entityTreeMap.EntityNames.Contains(rootEntityName) &&
                  !rootEntityName.Equals(wrapperEntityName, StringComparison.OrdinalIgnoreCase))
            {
                if (modelTreeMap.DictionaryTree.TryGetValue(rootEntityName, out var model))
                {
                    rootEntityName = model.ParentName;
                    transformedToParent = true;
                }
                else
                {
                    rootEntityName = wrapperEntityName;
                    transformedToParent = true;
                }
            }

            SqlMutationCompiler.CompileMutations(
                ctx,
                graphQlSelection,
                entityTreeMap,
                modelTreeMap,
                rootModelName,
                rootEntityName,
                wrapperEntityName,
                models);

            SqlQueryCompiler.CompileQuery<D, S>(
                ctx,
                graphQlSelection,
                entityTreeMap,
                modelTreeMap,
                rootEntityName,
                wrapperEntityName,
                transformedToParent);

            if (ctx.HasPagination || ctx.HasSorting)
            {
                ctx.SelectSql = SqlHelper.HandleQueryClause(
                    entityTreeMap.DictionaryTree[rootEntityName],
                    ctx.SelectSql,
                    ctx.OrderBy,
                    ctx.Pagination,
                    hasTotalCount: false);
            }

            return new SqlStructure
            {
                SqlQuery = ctx.SelectSql,
                SqlUpsert = ctx.UpsertSql,
                Pagination = ctx.Pagination,
                HasTotalCount = false
            };
        }

        public static void GetFieldsWhere(
            Dictionary<string, NodeTree> trees,
            Dictionary<string, SqlNode> linkEntityDictionaryTreeNode,
            Dictionary<string, SqlNode> linkModelDictionaryTreeNode,
            List<string> whereFields,
            Dictionary<string, string> sqlWhereStatement,
            ISyntaxNode whereNode,
            string entityName,
            string rootEntityName,
            string wrapperEntityName,
            string clauseCondition,
            List<string> clauseType,
            Dictionary<string, List<string>> permission = null)
        {
            if (whereNode == null || string.IsNullOrWhiteSpace(entityName))
                return;

            foreach (var wNode in whereNode.GetNodes())
            {
                var currentEntity = entityName;

                if (!string.IsNullOrEmpty(wrapperEntityName) &&
                    wrapperEntityName.Equals(currentEntity, StringComparison.OrdinalIgnoreCase))
                {
                    currentEntity = rootEntityName;
                }

                var nodeText = wNode.ToString() ?? string.Empty;
                if (nodeText.TrimStart().StartsWith("and:", StringComparison.OrdinalIgnoreCase) ||
                    nodeText.TrimStart().StartsWith("or:", StringComparison.OrdinalIgnoreCase))
                {
                    clauseCondition = nodeText.Split('{')[0]
                        .Replace(":", "").ToUpperInvariant();
                }

                if (nodeText.Contains("{") &&
                    nodeText.Contains(":") &&
                    nodeText.Split(':').Length == 3)
                {
                    var parts = nodeText.Split(new[] { ':' }, 3);
                    var columnName = parts[0];

                    if (!columnName.Contains("{") &&
                        linkModelDictionaryTreeNode.TryGetValue($"{currentEntity}~{columnName}", out var keyNode))
                    {
                        var path = keyNode.LinkKeys.FirstOrDefault()?.To;
                        if (!string.IsNullOrEmpty(path))
                            whereFields.Add(path.Replace('~', '.'));
                    }
                }

                foreach (var node in wNode.GetNodes().ToList())
                {
                    var text = node.ToString() ?? string.Empty;

                    if (!text.Contains("{") && text.Contains(":"))
                    {
                        var parts = text.Split(new[] { ':' }, 2);

                        if (parts.Length == 2 &&
                            !parts[1].Contains("DESC", StringComparison.OrdinalIgnoreCase) &&
                            !parts[1].Contains("ASC", StringComparison.OrdinalIgnoreCase) &&
                            clauseType.Contains(parts[0]))
                        {
                            if (whereFields.Count == 0)
                                continue;

                            var filterType  = parts[0];
                            var filterValue = parts[1].Trim().Trim('"');

                            var lastParts = whereFields.Last().Split('.');
                            var fieldName = lastParts.Length > 1 ? lastParts[1] : lastParts[0];

                            var filters = SqlGraphQLHelper.ProcessFilter(
                                trees[currentEntity],
                                linkModelDictionaryTreeNode,
                                fieldName,
                                MapFilterOperator(filterType),
                                filterValue,
                                clauseCondition);

                            if (filters != null && filters.Count > 0)
                            {
                                var clauseSql = string.Join(" AND ", filters);
                                if (!sqlWhereStatement.ContainsKey(currentEntity))
                                    sqlWhereStatement[currentEntity] = clauseSql;
                                else
                                    sqlWhereStatement[currentEntity] += " AND " + clauseSql;
                            }

                            clauseCondition = string.Empty;
                        }
                    }
                }

                GetFieldsWhere(
                    trees,
                    linkEntityDictionaryTreeNode,
                    linkModelDictionaryTreeNode,
                    whereFields,
                    sqlWhereStatement,
                    wNode,
                    currentEntity,
                    rootEntityName,
                    wrapperEntityName,
                    clauseCondition,
                    clauseType,
                    permission);
            }
        }

        private static string MapFilterOperator(string clause) => clause switch
        {
            "eq"  => "=",
            "neq" => "<>",
            "in"  => "in",
            _     => "="
        };
    }
}
