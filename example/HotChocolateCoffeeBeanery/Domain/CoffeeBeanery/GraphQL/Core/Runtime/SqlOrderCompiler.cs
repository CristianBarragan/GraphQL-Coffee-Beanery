using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlOrderCompiler
    {
        public static void Compile(
            SqlCompilationContext context,
            Dictionary<string, NodeTree> trees,
            ISyntaxNode orderNode,
            NodeTree entity,
            Dictionary<string, SqlNode> nodeDict) 
        {
            var sqlOrderStatement = new Dictionary<string, string>();
            
            GetFieldsOrdering(trees, orderNode, entity, nodeDict, sqlOrderStatement);
            
            context.SqlOrderStatements = sqlOrderStatement;
        }

        private static void GetFieldsOrdering(
            Dictionary<string, NodeTree> modelTrees,
            ISyntaxNode orderNode,
            NodeTree currentEntityTree,
            Dictionary<string, SqlNode> modelNodes,
            Dictionary<string, string> sqlOrderStatement)
        {
            foreach (var oNode in orderNode.GetNodes())
            {
                var activeEntityTree = currentEntityTree;
                var oNodeStr = oNode.ToString();

                if (oNodeStr.Contains("{") && oNodeStr[0] != '{' && oNodeStr.Contains(":"))
                {
                    var entityName = oNodeStr.Split(":")[0].Trim();
                    var matched = modelTrees.Values.FirstOrDefault(t =>
                        t.Name.Equals(entityName, StringComparison.OrdinalIgnoreCase) ||
                        t.Alias.Equals(entityName, StringComparison.OrdinalIgnoreCase) ||
                        t.Name.Equals(entityName.Replace("_", ""), StringComparison.OrdinalIgnoreCase) ||
                        t.Alias.Equals(entityName.Replace("_", ""), StringComparison.OrdinalIgnoreCase));

                    if (matched != null)
                        activeEntityTree = matched;
                }

                if (!oNodeStr.Contains("{") && oNodeStr.Contains(":"))
                {
                    var parts = oNodeStr.Split(":");
                    var field = parts[0].Trim();
                    var direction = parts[1].Trim();

                    if (direction.Contains("DESC") || direction.Contains("ASC"))
                    {
                        var sortResult = HandleSort(activeEntityTree, field, direction, modelNodes);
                        if (!string.IsNullOrEmpty(sortResult))
                        {
                            if (sqlOrderStatement.TryGetValue(activeEntityTree.Alias, out var existing))
                                sqlOrderStatement[activeEntityTree.Alias] = existing + ", " + sortResult;
                            else
                                sqlOrderStatement[activeEntityTree.Alias] = sortResult;
                        }
                    }
                }

                GetFieldsOrdering(modelTrees, oNode, activeEntityTree, modelNodes, sqlOrderStatement);
            }
        }

        private static string HandleSort(
            NodeTree nodeTree,
            string field,
            string sortClause,
            Dictionary<string, SqlNode> modelNodes)
        {
            var match = modelNodes.FirstOrDefault(kvp =>
                kvp.Key.StartsWith(nodeTree.Alias, StringComparison.OrdinalIgnoreCase) &&
                kvp.Key.EndsWith($"~{field}", StringComparison.OrdinalIgnoreCase));

            if (match.Value != null)
                return $"~*~.\"{match.Value.Column.ToSnakeCase(nodeTree.Id)}\" {sortClause.Trim()}";

            return string.Empty;
        }
    }
}