using System.Linq;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Helper;
using CoffeeBeanery.GraphQL.Helper;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlWhereCompiler
    {
        public static void Compile(
            SqlCompilationContext ctx,
            ISelection selection,
            NodeTree rootTree,
            string wrapperEntityName,
            Dictionary<string, string> sqlWhereStatement)
        {
            var whereFields = new List<string>();
            GetFieldsWhere(SqlNodeRegistry.ModelTrees, rootTree, SqlNodeRegistry.EntityNodes,
                SqlNodeRegistry.ModelNodes,
                whereFields, sqlWhereStatement, selection.SyntaxNode.Arguments
                    .FirstOrDefault(a => a.Name.Value.Matches("where")),
                SqlNodeRegistry.ModelTrees.Last().Value.Name, wrapperEntityName,
                string.Empty, Entity.ClauseTypes, default);
        }

        public static void GetFieldsWhere(Dictionary<string, NodeTree> trees, NodeTree rootTree,
            Dictionary<string, SqlNode> linkEntityDictionaryTreeNode,
            Dictionary<string, SqlNode> linkModelDictionaryTreeNode, List<string> whereFields,
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

                currentEntity = trees.Keys.FirstOrDefault(e => e.ToString()
                    .Matches(wNode.ToString().Split(":")[0]));

                if (string.IsNullOrEmpty(currentEntity) || currentEntity.Matches(rootEntityName))
                {
                    currentEntity = entityName;
                }

                if (whereNode.ToString().TrimStart(' ').StartsWith("and:") ||
                    whereNode.ToString().TrimStart(' ').StartsWith("or:"))
                {
                    clauseCondition = whereNode.ToString().Split("{")[0].Replace(":", "").ToUpper();
                }

                if (wNode.ToString().Contains("{") && wNode.ToString().Contains(":") &&
                    wNode.ToString().Split(":").Length == 3)
                {
                    var column = wNode.ToString().Split(":")[0];

                    if (!column.Contains("{"))
                    {
                        if (linkModelDictionaryTreeNode.TryGetValue($"{currentEntity}~{column}",
                                out var currentKeyValueNode))
                        {
                            var fieldValue = currentKeyValueNode.RelationshipKey.Replace('~', '.');
                            currentEntity = $"{currentKeyValueNode.RelationshipKey.Split('~')[0]}";
                            whereFields.Add(fieldValue);
                        }
                    }
                }

                foreach (var node in wNode.GetNodes().ToList())
                {
                    if (!node.ToString().Contains("{") && node.ToString().Contains(":") &&
                        node.ToString().Split(":").Length == 2)
                    {
                        var column = node.ToString().Split(":");
                        if (!column[1].Contains("DESC") && !column[1].Contains("ASC") &&
                            clauseType.Contains(column[0]))
                        {
                            if (whereFields.Count == 0)
                            {
                                continue;
                            }

                            var clauseValue = column[1].Trim().Trim('"');
                            var fieldParts = whereFields.Last().Split('.');
                            var currentNodeTree = trees[currentEntity];
                            var field = fieldParts[1];

                            switch (column[0])
                            {
                                case "eq":
                                {
                                    var clause = SqlGraphQLHelper
                                        .ProcessFilter(currentNodeTree, linkEntityDictionaryTreeNode,
                                            field, "=",
                                            clauseValue, clauseCondition);
                                    AddToDictionary(sqlWhereStatement, clause, field, trees);
                                    break;
                                }
                                case "neq":
                                {
                                    var clause = SqlGraphQLHelper
                                        .ProcessFilter(currentNodeTree, linkEntityDictionaryTreeNode, field, "<>",
                                            clauseValue, clauseCondition);
                                    AddToDictionary(sqlWhereStatement, clause, field, trees);
                                    break;
                                }
                                case "in":
                                {
                                    clauseValue = "(" + string.Join(',',
                                        column[1].Replace("[", "").Replace("]", "").Split(',')
                                            .Select(v => $"'{v.Trim()}'")) + ")";
                                    var clause = SqlGraphQLHelper
                                        .ProcessFilter(currentNodeTree, linkEntityDictionaryTreeNode,
                                            field, "in", clauseValue, clauseCondition);
                                    AddToDictionary(sqlWhereStatement, clause, field, trees);
                                    break;
                                }
                            }

                            clauseCondition = string.Empty;
                        }
                    }
                }

                GetFieldsWhere(trees, rootTree, linkEntityDictionaryTreeNode, linkModelDictionaryTreeNode,
                    whereFields,
                    sqlWhereStatement,
                    wNode,
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