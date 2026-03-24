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
            string entity,
            Dictionary<string, SqlNode> nodeDict)
        {
            ctx.SqlOrderStatement = GetFieldsOrdering(trees, orderNode.SyntaxNode, entity, nodeDict);
        }
        
        private static string GetFieldsOrdering(Dictionary<string, NodeTree> trees, ISyntaxNode orderNode, string entity,
            Dictionary<string, SqlNode> linkModelDictionaryTree)
        {
            var orderString = string.Empty;
            foreach (var oNode in orderNode.GetNodes())
            {
                var currentEntity = entity;
                if (oNode.ToString().Contains("{") && oNode.ToString()[0] != '{' && oNode.ToString().Contains(":"))
                {
                    currentEntity = oNode.ToString().Split(":")[0];
                }

                if (!oNode.ToString().Contains("{") && oNode.ToString().Contains(":"))
                {
                    var column = oNode.ToString().Split(":");
                    if ((column[1].Contains("DESC") || column[1].Contains("ASC")) &&
                        trees.ContainsKey(currentEntity))
                    {
                        var currentNodeTree = trees[currentEntity];
                        orderString += HandleSort(currentNodeTree, column[0], column[1], linkModelDictionaryTree);
                    }
                }

                orderString += $", {GetFieldsOrdering(trees, oNode, currentEntity, linkModelDictionaryTree)}";
            }

            return orderString;
        }
        
        private static string HandleSort(NodeTree nodeTree, string field, string sortClause, Dictionary<string, SqlNode> linkModelDictionaryTree)
        {
            if (linkModelDictionaryTree.TryGetValue($"{nodeTree.Name}~{field}", out var sqlNodeTo))
            {
                return $" ~*~.{sqlNodeTo.RelationshipKey.Split('~')[0]} ORDER BY {sortClause},";
            }
            return string.Empty;
        }
    }
}