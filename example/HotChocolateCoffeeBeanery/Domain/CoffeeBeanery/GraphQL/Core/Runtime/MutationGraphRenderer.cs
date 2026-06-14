using System.Text;
using CoffeeBeanery.GraphQL.Core.Contracts;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class MutationGraphRenderer
{
    public static string Render(MutationGraphPlan plan)
    {
        var sqlParts = new List<string>();

        foreach (var node in plan.Nodes.Values)
        {
            if (node.Fields.Count == 0)
                continue;

            var schema = node.Schema;

            var columns = node.Fields
                .Select(f => $"\"{f.Column}\"")
                .ToList();

            var values = node.Fields
                .Select(f => FormatValue(f.Value))
                .ToList();

            var sql =
                $"INSERT INTO \"{schema}\".\"{node.Table}\" " +
                $"( {string.Join(", ", columns)} ) " +
                $"VALUES ( {string.Join(", ", values)} ) " +
                $"ON CONFLICT DO NOTHING";

            sqlParts.Add(sql);
        }

        // OPTIONAL: edges (if you later want FK resolution / graph merges)
        foreach (var edge in plan.Edges)
        {
            sqlParts.Add(
                $"-- EDGE {edge.FromAlias} -> {edge.ToAlias} " +
                $"({edge.FromColumn} -> {edge.ToColumn})");
        }

        return string.Join(";\n", sqlParts);
    }

    private static string FormatValue(object? value) => value switch
    {
        null => "NULL",
        string s => $"'{s.Replace("'", "''")}'",
        _ => value.ToString()!
    };
}