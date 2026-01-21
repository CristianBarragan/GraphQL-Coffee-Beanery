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
                if (arg.Name.Value.Contains("order"))
                {
                    var raw = arg.Value?.ToString()?.Trim('{', '}', ' ') ?? "";
                    var parts = raw.Split(':');
                    if (parts.Length == 2)
                    {
                        ctx.AddOrder($"{parts[0].Trim()} {parts[1].Trim()}");
                    }
                }
            }
        }
    }
}