using System.Collections.Generic;
using System.Linq;
using System.Text;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlSelectBuilder
    {
        public static string Build(
            SqlCompilationContext ctx,
            NodeTree rootTree,
            Dictionary<string, SqlNode> nodeDict,
            Dictionary<string, SqlNode> edgeDict)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");

            if (!ctx.SelectSqlFields.Any())
                sb.Append("* ");
            else
                sb.Append(string.Join(", ", ctx.SelectSqlFields));

            sb.Append($" FROM \"{rootTree.Schema}\".\"{rootTree.Name}\" {rootTree.Name} ");

            // EDGE joins (INNER)
            foreach (var edge in edgeDict.Values)
            {
                if (string.IsNullOrEmpty(edge.Relationship))
                    continue;

                sb.Append($"INNER JOIN \"{edge.Schema}\".\"{edge.Relationship}\" {edge.Relationship} " +
                          $"ON {rootTree.Name}.\"{edge.JoinColumnFrom}\" = {edge.Relationship}.\"{edge.JoinColumnTo}\" ");
            }

            // NODE joins (LEFT)
            foreach (var node in nodeDict.Values)
            {
                if (string.IsNullOrEmpty(node.JoinTable))
                    continue;

                sb.Append($"LEFT JOIN \"{node.Schema}\".\"{node.JoinTable}\" {node.JoinTable} " +
                          $"ON {rootTree.Name}.\"{node.JoinColumnFrom}\" = {node.JoinTable}.\"{node.JoinColumnTo}\" ");
            }

            if (!string.IsNullOrEmpty(ctx.Where))
                sb.Append($" WHERE {ctx.Where}");

            if (!string.IsNullOrEmpty(ctx.OrderBy))
                sb.Append($" ORDER BY {ctx.OrderBy}");

            return sb.ToString();
        }
    }
}