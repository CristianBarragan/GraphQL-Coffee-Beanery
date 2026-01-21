using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Language;
using System.Collections.Generic;

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

            VisitWhereNodes(
                whereArg.Value,
                rootEntity,
                nodes,
                ctx,
                string.Empty);
        }

        private static void VisitWhereNodes(
            ISyntaxNode node,
            string currentEntity,
            Dictionary<string, SqlNode> nodes,
            SqlCompilationContext ctx)
        {
            foreach (var child in node.GetNodes())
            {
                var name = child.ToString().Split(':')[0].Trim();

                // entity transition
                if (nodes.Keys.Any(k => k.StartsWith($"{currentEntity}~{name}")))
                {
                    VisitWhereNodes(child, name, nodes, ctx);
                    continue;
                }

                if (child.GetNodes().Any()) 
                {
                    VisitWhereNodes(child, currentEntity, nodes, ctx);
                    continue;
                }

                if (!TryParseFilter(child.ToString(), out var field, out var op, out var val))
                    continue;

                var key = $"{currentEntity}~{field}";
                if (!nodes.TryGetValue(key, out var sqlNode)) continue;

                ctx.AddWhere(
                    sqlNode.EntityName,
                    $"{sqlNode.EntityName}.\"{sqlNode.ColumnName}\" {op} {val}",
                    field,
                    val);
            }
        }

        private static bool TryParseFilter(
            string text,
            out string field,
            out string op,
            out string value)
        {
            field = ""; op = ""; value = "";
            if (!text.Contains(':')) return false;

            var parts = text.Split(':');
            field = parts[0].Trim();
            value = parts.Length > 1
                ? $"'{parts[1].Trim().Trim('"')}'"
                : "''";

            op = field.EndsWith("_neq") ? "<>" :
                 field.EndsWith("_in") ? "IN" : "=";

            return true;
        }
    }
}
