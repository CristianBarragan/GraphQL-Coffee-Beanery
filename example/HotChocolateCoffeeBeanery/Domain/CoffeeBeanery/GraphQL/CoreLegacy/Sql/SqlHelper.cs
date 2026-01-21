using System.Linq;
using System.Text;
using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public static class SqlHelper
    {
        public static string GenerateMerge(
            Dictionary<string, SqlNode> nodes,
            string rootEntityName)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"-- MERGE for {rootEntityName}");

            // simplistic MERGE pseudocode
            foreach (var node in nodes.Values.Where(n => n.SqlNodeType != SqlNodeType.None))
            {
                var keys = string.Join(", ", node.UpsertKeys.Select(u => $"{u.Entity}.{u.Column}"));
                sb.AppendLine($"MERGE INTO {node.EntityName} USING ... ON ({keys});");
            }

            return sb.ToString();
        }
    }
}