using System.Linq;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlWhereCompiler
    {
        public static void Compile(
            SqlCompilationContext ctx,
            ISelection rootSelection,
            NodeTree rootTree,
            Dictionary<string, SqlNode> nodeDict)
        {
            var whereArg = rootSelection.SyntaxNode.Arguments
                .FirstOrDefault(a => a.Name.Value == "where");

            if (whereArg == null)
                return;

            // This is a simplified parser.
            // Your current logic can be ported here.
            // For now we support only `field: value`

            var raw = whereArg.Value?.ToString() ?? "";
            raw = raw.Trim('{', '}', ' ');

            var parts = raw.Split(':');
            if (parts.Length != 2)
                return;

            var field = parts[0].Trim();
            var value = parts[1].Trim().Trim('"');

            if (nodeDict.TryGetValue($"{rootTree.Name}~{field}", out var node))
            {
                ctx.Where = $"{rootTree.Name}.\"{node.Column}\" = '{value}'";
            }
        }
    }
}