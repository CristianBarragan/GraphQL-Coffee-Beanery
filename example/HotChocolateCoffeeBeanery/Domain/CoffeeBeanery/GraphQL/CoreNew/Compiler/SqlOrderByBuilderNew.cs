using CoffeeBeanery.GraphQL.CoreNew.Sql;
using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.CoreNew.Compiler
{
    public static class SqlOrderByBuilderNew
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
                    var raw = arg.Value.ToString().Trim('{', '}');
                    var parts = raw.Split(':');
                    if (parts.Length == 2)
                    {
                        ctx.OrderByClauses.Add(
                            $"{parts[0]} {parts[1]}");
                        ctx.HasSorting = true;
                    }
                }
            }
        }
    }
}
