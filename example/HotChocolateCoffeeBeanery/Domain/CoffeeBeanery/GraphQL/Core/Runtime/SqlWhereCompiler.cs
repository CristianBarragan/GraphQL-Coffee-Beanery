using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Helper;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlWhereCompiler
    {
        public static void Compile(SqlCompilationContext context, Dictionary<string, NodeTree> modelTrees,
            Dictionary<string, NodeTree> entityTrees, Dictionary<string, SqlNode> modelsSqlNodes, Dictionary<string, SqlNode> entitiesSqlNodes,
            ISelection selection,
            NodeTree rootTree,
            string wrapperEntityName,
            Dictionary<string, string> sqlWhereStatement)
        {
            GetFieldsWhere(modelTrees, entityTrees, rootTree, modelsSqlNodes, entitiesSqlNodes,
                sqlWhereStatement, selection.SyntaxNode.Arguments.FirstOrDefault(a => a.Name.Value.Matches("where")),
                SqlNodeRegistry.ModelTrees.Last().Value.Name, wrapperEntityName,
                string.Empty, Entity.ClauseTypes, default);
            context.SqlWhereStatement = sqlWhereStatement;
        }

        public static void GetFieldsWhere(Dictionary<string, NodeTree> modelTrees,
            Dictionary<string, NodeTree> entityTrees,
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
            {
                return;
            }

            foreach (var wNode in whereNode.GetNodes())
            {
                if (wrapperEntityName.Matches(entityName))
                {
                    entityName = rootEntityName;
                }

                var currentEntity = entityName;
                
                if (whereNode.ToString().TrimStart(' ').StartsWith("and:") ||
                    whereNode.ToString().TrimStart(' ').StartsWith("or:"))
                {
                    clauseCondition = whereNode.ToString().Split("{")[0].Replace(":", "").ToUpper();
                    clauseCondition = clauseCondition.Matches("AND") ? "@" : "$";
                }

                var whereArray = whereNode.ToString().Split(":");

                if (whereNode.ToString().Contains("{") && whereNode.ToString().Contains(":") &&
                    whereArray.Length == 5)
                {
                    var rootTreeTemp = modelTrees.FirstOrDefault(e => e.Value.Name.Matches(wNode.ToString().Split(":")[0].Split(":")[0])).Value;

                    if (rootTreeTemp != null)
                    {
                        rootTree = rootTreeTemp;

                        if (wNode.ToString().Split(":").Length <= 2)
                        {
                            continue;
                        }
                        
                        var columnName = wNode.ToString().Split(":")[2].Replace(" {", "").Trim().ToUpperCamelCase();
                        
                        if (modelsSqlNodes.TryGetValue($"{rootTree.Alias}~{rootTree.Name}~{columnName}",
                                out var currentKeyValueNode))
                        {
                            var condition = string.Empty;
                            
                            foreach (var node in wNode.GetNodes().ToList())
                            {
                                if (node.ToString().Contains("{") && node.ToString().Contains(":") &&
                                    node.ToString().Split(":").Length == 4)
                                {
                                    var column = node.ToString().Split(":").Select(a => a.CleanJsonString()).ToList();
                                    
                                    if (!column[1].Contains("DESC") && !column[1].Contains("ASC") &&
                                        clauseType.Contains(column[2]))
                                    {
                                        var clauseValue = column[3];
                                        string enumeration;
                                        var enumValue = currentKeyValueNode.FromEnumeration.FirstOrDefault(a => a.Key.Matches(clauseValue));
                                        if (!string.IsNullOrEmpty(enumValue.Key))
                                        {
                                            var toEnum = currentKeyValueNode.ToEnumeration.FirstOrDefault(e =>
                                                e.Key.Matches(enumValue.Key)).Value;
                                            enumeration = toEnum.ToString();
                                        }
                                        else
                                        {
                                            enumeration = string.Empty;
                                        }

                                        switch (column[2])
                                        {
                                            case "eq":
                                            {
                                                if (clauseValue.Matches("null"))
                                                {
                                                    condition = $" {clauseCondition} ( ~.\"{currentKeyValueNode.Column}\" IS NULL ";
                                                    break;
                                                }

                                                condition = $" {clauseCondition} ( ~.\"{currentKeyValueNode.Column}\" = '{(string.IsNullOrEmpty(enumeration) ? clauseValue : enumeration)}' ";
                                                break;
                                            }
                                            case "neq":
                                            {
                                                if (clauseValue.Matches("null"))
                                                {
                                                    condition = $" {clauseCondition} ( ~.\"{currentKeyValueNode.Column}\" IS NOT NULL ";
                                                    break;
                                                }

                                                condition = $" {clauseCondition} ( ~.\"{currentKeyValueNode.Column}\" <> '{(string.IsNullOrEmpty(enumeration) ? clauseValue : enumeration)}' ";
                                                break;
                                            }
                                            case "in":
                                            {
                                                var inValues = string.Empty;
                                                foreach (var val in clauseValue.Split(','))
                                                {
                                                    var valAux = val.Sanitize().Replace("(", "").Replace(")", "").ToUpperCamelCase();
                                                    inValues += $"'{(string.IsNullOrEmpty(enumeration) ? valAux : enumeration)}'" + ",";
                                                }

                                                condition = $" {clauseCondition} ( ~.\"{currentKeyValueNode.Column}\" in ({inValues.Substring(0, inValues.Length - 1)})";
                                                break;
                                            }
                                        }
                                    }
                                }
                            }
                            
                            foreach (var linkey in currentKeyValueNode.LinkKeys)
                            {
                                if (sqlWhereStatement.TryGetValue(linkey.AliasTo, out var currentCondition) &&
                                    entitiesSqlNodes.Any(a => a.Value.Alias.Matches(linkey.AliasTo) && a.Value.Column.Matches(currentKeyValueNode.Column)))
                                {
                                    if (!currentCondition.Matches(condition))
                                    {
                                        sqlWhereStatement[linkey.AliasTo] += condition + " ) ";    
                                    }
                                }
                                else if (entitiesSqlNodes.Any(a => a.Value.Alias.Matches(linkey.AliasTo) && a.Value.Column.Matches(currentKeyValueNode.Column)))
                                {
                                    sqlWhereStatement.Add(linkey.AliasTo, condition);
                                }
                            }
                        }   
                    }
                }

                GetFieldsWhere(modelTrees, entityTrees, rootTree, modelsSqlNodes, entitiesSqlNodes, sqlWhereStatement, wNode,
                    currentEntity, wrapperEntityName, clauseCondition, clauseType, permission);
            }
        }
        
        private static void AddToDictionary(Dictionary<string, string> dictionary,
            List<string> values, string field, Dictionary<string, NodeTree> trees)
        {
            var entitiesWithColumn = trees.Values.Where(a => a.Mapping.Any(b => b.DestinationName.Matches(field))).ToList();

            foreach (var entity in entitiesWithColumn)
            {
                foreach (var value in values)
                {
                    if (!dictionary.TryGetValue(entity.Name, out var _))
                    {
                        dictionary.Add(entity.Name, value);
                    }
                    else
                    {
                        dictionary[entity.Name] += " " + value;
                    }
                }
            }
        }
        
        private static List<string> ProcessFilter(NodeTree nodeTree,
        Dictionary<string, SqlNode> linkModelDictionaryTreeNode, string field, string filterType, string value,
        string filterCondition)
        {
            var enumeration = string.Empty;
            var conditions = new List<string>();

            if (string.IsNullOrEmpty(field) ||
                string.IsNullOrEmpty(filterType))
            {
                return conditions;
            }

            if (linkModelDictionaryTreeNode.TryGetValue($"{nodeTree.Name}~{field}", out var sqlNodeTo))
            {
                var enumValue = sqlNodeTo.FromEnumeration.FirstOrDefault(a => a.Key.Matches(value));
                if (!string.IsNullOrEmpty(enumValue.Key))
                {
                    var toEnum = sqlNodeTo.ToEnumeration.FirstOrDefault(e =>
                        e.Key.Matches(enumValue.Key)).Value;
                    enumeration = toEnum.ToString();
                }
                else
                {
                    enumeration = string.Empty;
                }

                switch (filterType)
                {
                    case "<>":

                        if (value.Matches("null"))
                        {
                            conditions.Add($" {filterCondition} ~.\"{sqlNodeTo.Column}\" IS NOT NULL ");
                            return conditions;
                        }

                        conditions.Add(
                            $" {filterCondition} ~.\"{sqlNodeTo.Column}\" <> '{(string.IsNullOrEmpty(enumeration) ? value : enumeration)}' ");
                        return conditions;

                    case "=":

                        if (value.Matches("null"))
                        {
                            conditions.Add($" {filterCondition} ~.\"{sqlNodeTo.Column}\" IS NULL ");
                            return conditions;
                        }

                        conditions.Add(
                            $" {filterCondition} ~.\"{sqlNodeTo.Column}\" = '{(string.IsNullOrEmpty(enumeration) ? value : enumeration)}' ");
                        return conditions;

                    case "in":
                        var inValues = string.Empty;
                        foreach (var val in value.Split(','))
                        {
                            var valAux = val.Sanitize().Replace("(", "").Replace(")", "").ToUpperCamelCase();
                            inValues += $"'{(string.IsNullOrEmpty(enumeration) ? valAux : enumeration)}'" + ",";
                        }

                        conditions.Add(
                            $" {filterCondition} ~.\"{sqlNodeTo.Column}\" in ({inValues.Substring(0, inValues.Length - 1)})");
                        return conditions;
                }
            }

            return conditions;
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