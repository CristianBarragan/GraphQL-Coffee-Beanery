using CoffeeBeanery.GraphQL.CoreNew.Sql;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.CoreNew.Compiler
{
    public static class SqlWhereBuilderNew
    {
        public static void BuildWhere<TModel>(
            SqlCompilationContext ctx,
            ISelection root,
            Dictionary<string, SqlNode> nodes,
            string rootEntity)
            where TModel : class
        {
            var arg = root.SyntaxNode.Arguments
                .FirstOrDefault(a => a.Name.Value == "where");

            if (arg == null)
                return;

            ProcessWhere(arg.Value, nodes, rootEntity, ctx);
        }

        private static void ProcessWhere(
            ISyntaxNode node,
            Dictionary<string, SqlNode> nodes,
            string currentEntity,
            SqlCompilationContext ctx)
        {
            foreach (var child in node.GetNodes())
            {
                if (!child.GetNodes().Any())
                {
                    var txt = child.ToString();
                    if (TryParseCondition(txt, out var field, out var op, out var val))
                    {
                        var key = $"{currentEntity}~{field}";
                        if (nodes.TryGetValue(key, out var n))
                        {
                            var clause = $"{n.EntityName}.\"{n.ColumnName}\" {op} @{field}";
                            ctx.AddWhere(n.EntityName, clause, field, val);
                        }
                    }
                }
                else ProcessWhere(child, nodes, currentEntity, ctx);
            }
        }

        private static bool TryParseCondition(
            string text,
            out string field,
            out string op,
            out string value)
        {
            field = ""; op = ""; value = "";
            if (!text.Contains(":")) return false;

            var parts = text.Trim().Split(':');
            field = parts[0];
            value = parts[1].Trim().Trim('"');

            op = "="; 
            if (parts[0].EndsWith("_neq")) op = "<>";
            else if (parts[0].EndsWith("_in")) op = "IN";

            return true;
        }
    }
}
