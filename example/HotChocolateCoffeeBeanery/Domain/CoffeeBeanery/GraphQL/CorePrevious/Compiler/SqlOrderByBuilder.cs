using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SqlOrderByBuilder
    {
        public static void BuildOrderBy(
            SqlCompilationContext ctx,
            ISelection root,
            Dictionary<string, SqlNode> nodes,
            string rootEntity)
        {
            foreach (var arg in root.SyntaxNode.Arguments)
            {
                if (!arg.Name.Value.Contains("order")) continue;

                var raw = arg.Value?.ToString()?.Trim('{', '}', ' ');
                if (string.IsNullOrWhiteSpace(raw)) continue;

                var parts = raw.Split(':');
                if (parts.Length != 2) continue;

                var field = parts[0].Trim();
                var direction = parts[1].Trim();

                var key = $"{rootEntity}~{field}";
                if (nodes.TryGetValue(key, out var node))
                {
                    ctx.AddOrder($"{node.EntityName}.\"{node.ColumnName}\" {direction}");
                }
            }
        }
    }
}