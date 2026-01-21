using System;
using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Helper
{
    public static class SqlGraphQLHelper
    {
        public static List<string> ProcessFilter(
            NodeTree nodeTree,
            Dictionary<string, SqlNode> linkModelDictionaryTreeNode,
            string field,
            string filterType,
            string value,
            string filterCondition)
        {
            if (string.IsNullOrEmpty(field) || string.IsNullOrEmpty(filterType))
                return new List<string>();

            var conditions = new List<string>();

            if (linkModelDictionaryTreeNode.TryGetValue($"{nodeTree.Name}~{field}", out var sqlNode))
            {
                var normalizedValue = NormalizeValue(value, sqlNode);

                switch (filterType.ToLowerInvariant())
                {
                    case "=":
                        conditions.Add(BuildEqualCondition(sqlNode, normalizedValue, filterCondition));
                        break;
                    case "<>":
                        conditions.Add(BuildNotEqualCondition(sqlNode, normalizedValue, filterCondition));
                        break;
                    case "in":
                        conditions.Add(BuildInCondition(sqlNode, normalizedValue, filterCondition));
                        break;
                }
            }

            return conditions;
        }

        private static string NormalizeValue(string value, SqlNode sqlNode)
        {
            if (string.Equals(value?.Trim(), "null", StringComparison.OrdinalIgnoreCase))
                return null;

            if (!string.IsNullOrEmpty(value) && sqlNode.FromEnumeration != null)
            {
                if (sqlNode.FromEnumeration.TryGetValue(value.Trim(), out var mapped))
                    return mapped.ToString();
            }

            return value;
        }

        private static string BuildEqualCondition(SqlNode node, string value, string cond)
        {
            if (string.IsNullOrEmpty(value))
                return $"{cond} ~.\"{node.Column}\" IS NULL";
            return $"{cond} ~.\"{node.Column}\" = '{value.Replace("'", "''")}'";
        }

        private static string BuildNotEqualCondition(SqlNode node, string value, string cond)
        {
            if (string.IsNullOrEmpty(value))
                return $"{cond} ~.\"{node.Column}\" IS NOT NULL";
            return $"{cond} ~.\"{node.Column}\" <> '{value.Replace("'", "''")}'";
        }

        private static string BuildInCondition(SqlNode node, string value, string cond)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            var raw = value.Trim();
            if (raw.StartsWith("(") && raw.EndsWith(")"))
                raw = raw[1..^1];

            var items = raw.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(x => $"'{x.Trim().Replace("'", "''")}'");

            return $"{cond} ~.\"{node.Column}\" IN ({string.Join(", ", items)})";
        }

        public static string HandleSort(
            NodeTree nodeTree,
            string field,
            string direction,
            Dictionary<string, SqlNode> linkModelDictionaryTreeNode)
        {
            var dir = direction.Trim().ToUpperInvariant().Contains("DESC") ? "DESC" : "ASC";

            if (linkModelDictionaryTreeNode.TryGetValue($"{nodeTree.Name}~{field}", out var node))
                return $" {nodeTree.Name}.\"{node.Column}\" {dir}, ";

            return $" {nodeTree.Name}.\"{field}\" {dir}, ";
        }
    }
}
