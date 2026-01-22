using System.Linq;
using System.Text;
using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlOrderCompiler
    {
        public static void Compile(
            SqlCompilationContext ctx,
            ISelection rootSelection,
            NodeTree rootTree,
            Dictionary<string, SqlNode> nodeDict)
        {
            var sb = new StringBuilder();

            foreach (var arg in rootSelection.SyntaxNode.Arguments)
            {
                if (arg.Name.Value.Contains("order"))
                {
                    var raw = arg.Value?.ToString() ?? "";
                    raw = raw.Trim('{', '}', ' ');

                    var parts = raw.Split(':').Select(p => p.Trim()).ToArray();
                    if (parts.Length == 2)
                    {
                        if (nodeDict.TryGetValue($"{rootTree.Name}~{parts[0]}", out var node))
                        {
                            sb.Append($"{rootTree.Name}.\"{node.Column}\" {parts[1]}, ");
                        }
                    }
                }
            }

            ctx.OrderBy = sb.ToString().TrimEnd(',', ' ');
        }
    }
}