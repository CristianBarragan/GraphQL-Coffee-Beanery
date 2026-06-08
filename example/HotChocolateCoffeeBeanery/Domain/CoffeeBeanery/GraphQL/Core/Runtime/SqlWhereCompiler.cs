using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Helper;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlWhereCompiler
    {
        public static void Compile(SqlCompilationContext context,
            Dictionary<string, SqlNode> modelsSqlNodes, Dictionary<string, SqlNode> entitiesSqlNodes,
            ISelection selection,
            NodeTree rootTree,
            string wrapperEntityName,
            Dictionary<string, string> sqlWhereStatement)
        {
            GetFieldsWhere(rootTree, modelsSqlNodes, entitiesSqlNodes,
                sqlWhereStatement, selection.SyntaxNode.Arguments.FirstOrDefault(a => a.Name.Value.Matches("where")),
                SqlNodeRegistry.ModelTrees.Last().Value.Name, wrapperEntityName,
                string.Empty, Entity.ClauseTypes, default);
            context.SqlWhereStatement = sqlWhereStatement;
        }

        public static void GetFieldsWhere(
            NodeTree rootTree,
            Dictionary<string, SqlNode> modelsSqlNodes,
            Dictionary<string, SqlNode> entitiesSqlNodes,
            Dictionary<string, string> sqlWhereStatement,
            ISyntaxNode whereNode, string rootEntityName, string wrapperEntityName,
            string clauseCondition,
            List<string> clauseType,
            Dictionary<string, List<string>> permission = null)
        {
            var entityName = rootTree.Name;

            if (whereNode == null || string.IsNullOrWhiteSpace(entityName))
                return;

            foreach (var wNode in whereNode.GetNodes())
            {
                if (wrapperEntityName.Matches(entityName))
                    entityName = rootEntityName;

                var currentEntity = entityName;

                if (whereNode.ToString().TrimStart(' ').StartsWith("and:") ||
                    whereNode.ToString().TrimStart(' ').StartsWith("or:"))
                {
                    clauseCondition = whereNode.ToString().Split("{")[0].Replace(":", "").ToUpper();
                    clauseCondition = clauseCondition.Matches("AND") ? "@" : "$";
                }

                var wNodeStr = wNode.ToString();
                var wNodeChildren = wNode.GetNodes().ToList();
                
                bool isFieldLevelNode = wNodeStr.Contains("{") &&
                                        wNodeStr.Contains(":") &&
                                        wNodeChildren.Count == 2 &&
                                        wNodeChildren[1].GetNodes().ToList().Count == 1 &&
                                        wNodeChildren[1].GetNodes().First()
                                            .ToString().Split(":").Length == 2;

                if (isFieldLevelNode)
                {
                    var fieldName = wNodeChildren[0].ToString().Trim();

                    var operatorNode = wNodeChildren[1].GetNodes().First().ToString();
                    var operatorParts = operatorNode.Split(":").Select(a => a.CleanJsonString()).ToList();

                    if (operatorParts.Count < 2)
                        goto recurse;

                    var op = operatorParts[0].Trim();
                    var clauseValue = operatorParts[1].Trim();

                    if (!clauseType.Contains(op))
                        goto recurse;

                    var matchingNode = modelsSqlNodes
                        .FirstOrDefault(kvp => kvp.Key.EndsWith($"~{fieldName}",
                            StringComparison.OrdinalIgnoreCase));

                    if (matchingNode.Value == null)
                        goto recurse;

                    var currentKeyValueNode = matchingNode.Value;

                    var enumeration = string.Empty;
                    var enumValue = currentKeyValueNode.FromEnumeration
                        .FirstOrDefault(a => a.Key.Matches(clauseValue));
                    if (!string.IsNullOrEmpty(enumValue.Key))
                    {
                        var toEnum = currentKeyValueNode.ToEnumeration
                            .FirstOrDefault(e => e.Key.Matches(enumValue.Key)).Value;
                        enumeration = toEnum.ToString() ?? string.Empty;
                    }

                    var resolvedValue = string.IsNullOrEmpty(enumeration) ? clauseValue : enumeration;

                    string condition = op switch
                    {
                        "eq" => clauseValue.Matches("null")
                            ? $" ( ~.\"{currentKeyValueNode.Column}\" IS NULL "
                            : $" ( ~.\"{currentKeyValueNode.Column}\" = '{resolvedValue}' ",
                        "neq" => clauseValue.Matches("null")
                            ? $" ( ~.\"{currentKeyValueNode.Column}\" IS NOT NULL "
                            : $" ( ~.\"{currentKeyValueNode.Column}\" <> '{resolvedValue}' ",
                        "in" => BuildInCondition(clauseValue, currentKeyValueNode, enumeration),
                        _ => string.Empty
                    };

                    if (string.IsNullOrEmpty(condition))
                        goto recurse;

                    var processedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                    foreach (var linkey in currentKeyValueNode.LinkKeys)
                    {
                        if (!processedAliases.Add(linkey.AliasTo))
                            continue;

                        var columnMatches = entitiesSqlNodes.Any(a =>
                            a.Value.Alias.Matches(linkey.AliasTo) &&
                            a.Value.Column.Matches(currentKeyValueNode.Column));

                        if (!columnMatches)
                            continue;

                        if (sqlWhereStatement.TryGetValue(linkey.AliasTo, out var currentCondition))
                        {
                            if (!currentCondition.Contains(condition))
                                sqlWhereStatement[linkey.AliasTo] += $" AND {condition} ) ";
                        }
                        else
                        {
                            sqlWhereStatement.Add(linkey.AliasTo, $"{condition} ) ");
                        }
                    }
                }
                
                recurse:
                GetFieldsWhere(rootTree, modelsSqlNodes, entitiesSqlNodes, sqlWhereStatement,
                    wNode, currentEntity, wrapperEntityName, clauseCondition, clauseType, permission);
            }
        }
        
        private static string BuildInCondition(string clauseValue, SqlNode node, string enumeration)
        {
            var inValues = string.Empty;
            foreach (var val in clauseValue.Split(','))
            {
                var valAux = val.Sanitize().Replace("(", "").Replace(")", "").ToUpperCamelCase();
                inValues += $"'{(string.IsNullOrEmpty(enumeration) ? valAux : enumeration)}'" + ",";
            }
            return $" ( ~.\"{node.Column}\" in ({inValues.TrimEnd(',')})";
        }
    }
    
    public class Entity
    {
        public static List<string> ClauseTypes = new List<string>()
        {
            "eq",
            "neq",
            "in",
            "any"
        };
    }
}