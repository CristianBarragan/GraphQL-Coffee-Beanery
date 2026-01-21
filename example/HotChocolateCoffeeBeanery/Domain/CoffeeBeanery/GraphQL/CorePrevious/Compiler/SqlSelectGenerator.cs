using System.Linq;
using System.Text;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SqlSelectGenerator
    {
        public static string BuildSelect(
            SqlCompilationContext ctx,
            Dictionary<string, SqlNode> nodes,
            NodeTree root)
        {
            var sb = new StringBuilder();

            var select = nodes.Values
                .Where(n => n.SqlNodeType == SqlNodeType.Select)
                .Select(n => $"{n.EntityName}.\"{n.ColumnName}\"");

            sb.Append("SELECT ");
            sb.Append(string.Join(",", select));
            sb.Append($" FROM \"{root.Schema}\".\"{root.Name}\" {root.Name}");

            foreach (var node in nodes.Values)
            {
                foreach (var link in node.LinkKeys)
                {
                    sb.Append(
                        $" LEFT JOIN {link.To.Split('~')[0]} ON " +
                        $"{link.From.Split('~')[0]}.\"Id\" = " +
                        $"{link.To.Split('~')[0]}.\"Id\"");
                }
            }

            if (ctx.TryGetWhere(root.Name, out var where))
                sb.Append(" WHERE " + where);

            if (ctx.HasSorting)
                sb.Append(" ORDER BY " + string.Join(",", ctx.OrderClauses));

            if (ctx.HasPagination && ctx.Pagination.First.HasValue)
                sb.Append(" LIMIT " + ctx.Pagination.First.Value);

            return sb.ToString();
        }
    }
}