using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlOrderCompiler
    {
        public static void Compile(
            SqlCompilationContext ctx,
            Dictionary<string, NodeTree> trees,
            ISelection orderNode,
            NodeTree entity,
            Dictionary<string, SqlNode> nodeDict)
        {
            ctx.SqlOrderStatement = GetFieldsOrdering(trees, orderNode.SyntaxNode, entity, nodeDict);
        }
        
        public static string GetFieldsOrdering(Dictionary<string, NodeTree> modelTrees,
            ISyntaxNode orderNode, NodeTree currentEntityTree, Dictionary<string, SqlNode> modelNodes)
        {
            var orderString = string.Empty;
            foreach (var oNode in orderNode.GetNodes())
            {
                var currentEntity = currentEntityTree.Name;
                if (oNode.ToString().Contains("{") && oNode.ToString()[0] != '{' &&
                    oNode.ToString().Contains(":"))
                {
                    currentEntity = oNode.ToString().Split(":")[0];
                }

                if (!oNode.ToString().Contains("{") && oNode.ToString().Contains(":"))
                {
                    var column = oNode.ToString().Split(":");
                    if ((column[1].Contains("DESC") || column[1].Contains("ASC")) &&
                        modelTrees.ContainsKey(currentEntity))
                    {
                        var currentNodeTree = modelTrees[currentEntity];
                        orderString +=
                            SqlGraphQlHelper.HandleSort(currentNodeTree, column[0],
                                column[1], modelNodes);
                    }
                }

                orderString +=
                    $", {GetFieldsOrdering(modelTrees, oNode, currentEntityTree, modelNodes)}";
            }

            return orderString;
        }
        
       private static string HandleSort(NodeTree nodeTree, string field, string sortClause, Dictionary<string, SqlNode> modelNodes)
        {
            if (modelNodes.TryGetValue($"{nodeTree.Name}~{field}", out var sqlNodeTo))
            {
                return $" ~*~.{sqlNodeTo.RelationshipKey.Split('~')[0]} ORDER BY {sortClause},";
            }
            return string.Empty;
        }
    }
}