using System.Text;
using CoffeeBeanery.GraphQL.CoreNew.Sql;

namespace CoffeeBeanery.GraphQL.CoreNew.Compiler
{
    public static class SqlSelectGeneratorNew
    {
        public static string BuildSelect(
            SqlCompilationContext ctx,
            Dictionary<string, SqlNode> nodes,
            string rootEntity)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");

            var selected = nodes.Values
                .Where(n => n.SqlNodeType == SqlNodeType.Select)
                .Select(n => $"{n.EntityName}.\"{n.ColumnName}\"");
            sb.Append(string.Join(",", selected));

            sb.Append($" FROM \"{rootEntity}\" {rootEntity}");

            foreach (var n in nodes.Values)
            {
                foreach (var link in n.LinkKeys)
                {
                    sb.Append($" LEFT JOIN {link.ToKey.Split('~')[0]} ON " +
                              $"{link.FromKey.Split('~')[0]}.\"{link.FromKey.Split('~')[1]}\" = " +
                              $"{link.ToKey.Split('~')[0]}.\"{link.ToKey.Split('~')[1]}\"");
                }
            }

            if (ctx.WhereClauses.TryGetValue(rootEntity, out var w))
                sb.Append(" WHERE " + w);

            if (ctx.HasSorting)
                sb.Append(" ORDER BY " + string.Join(",", ctx.OrderByClauses));

            if (ctx.HasPagination)
                sb.Append(" LIMIT " + ctx.Pagination.First);

            return sb.ToString();
        }
    }
}
