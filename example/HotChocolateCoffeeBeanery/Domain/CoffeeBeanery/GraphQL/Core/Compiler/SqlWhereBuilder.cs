using System.Linq;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SqlWhereBuilder
    {
        public static void BuildWhere<TModel>(
            SqlCompilationContext ctx,
            ISelection rootSelection,
            Dictionary<string, SqlNode> nodes,
            string rootEntity)
            where TModel : class
        {
            var whereArg = rootSelection.SyntaxNode.Arguments
                .FirstOrDefault(a => a.Name.Value == "where");

            if (whereArg == null) return;

            VisitWhereNodes(whereArg.Value, rootEntity, nodes, ctx);
        }

        private static void VisitWhereNodes(
            ISyntaxNode node,
            string currentEntity,
            Dictionary<string, SqlNode> nodes,
            SqlCompilationContext ctx)
        {
            foreach (var child in node.GetNodes())
            {
                if (child.GetNodes().Count() == 0)
                {
                    var text = child.ToString().Trim();
                    if (TryParseFilter(text, out var field, out var op, out var val))
                    {
                        var key = $"{currentEntity}~{field}";
                        if (nodes.TryGetValue(key, out var sqlNode))
                        {
                            var clause = $"{sqlNode.EntityName}.\"{sqlNode.ColumnName}\" {op} {val}";
                            ctx.AddWhere(sqlNode.EntityName, clause);
                        }
                    }
                }
                else
                {
                    VisitWhereNodes(child, currentEntity, nodes, ctx);
                }
            }
        }

        private static bool TryParseFilter(string text, out string field, out string op, out string value)
        {
            field = ""; op = ""; value = "";
            if (!text.Contains(':')) return false;

            var parts = text.Split(':');
            field = parts[0].Trim();
            value = parts.Length > 1 ? $"'{parts[1].Trim().Trim('"')}'" : "''";

            op = field.EndsWith("_neq") ? "<>" :
                 field.EndsWith("_in") ? "IN" : "=";

            return true;
        }
    }
}
