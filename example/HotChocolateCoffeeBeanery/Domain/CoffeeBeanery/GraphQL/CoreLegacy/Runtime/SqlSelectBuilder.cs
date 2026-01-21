using System.Linq;
using System.Text;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlSelectBuilder
    {
        public static string Build(
            SqlCompilationContext ctx,
            Dictionary<string, NodeTree> entityTrees,
            List<string> entityNames,
            string rootEntityName)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");

            if (ctx.UpsertNodes.Count == 0)
            {
                sb.Append("* ");
            }
            else
            {
                sb.Append(string.Join(", ",
                    ctx.UpsertNodes.Select(kv =>
                        $"{kv.Value.Entity}.\"{kv.Value.Column}\"")));
            }

            sb.Append($" FROM \"{entityTrees[rootEntityName].Schema}\".\"{rootEntityName}\" {rootEntityName}");

            if (ctx.Where.TryGetValue(rootEntityName, out var whereClause) &&
                !string.IsNullOrEmpty(whereClause))
            {
                sb.Append(" WHERE ");
                sb.Append(whereClause);
            }

            if (!string.IsNullOrEmpty(ctx.OrderBy))
            {
                sb.Append(" ORDER BY ");
                sb.Append(ctx.OrderBy);
            }

            return sb.ToString();
        }
    }
}