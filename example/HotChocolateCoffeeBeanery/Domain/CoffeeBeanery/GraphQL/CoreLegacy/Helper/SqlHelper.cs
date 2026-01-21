using System.Collections.Generic;
using System.Text;
using CoffeeBeanery.GraphQL.Core.Sql;

public static class SqlHelper
{
    public static string GenerateMerge<TModel>(
        Dictionary<string, SqlNode> nodes,
        string rootEntity,
        object model)
    {
        var sb = new StringBuilder();

        sb.AppendLine($"-- MERGE statements for {rootEntity}");

        foreach (var node in nodes.Values)
        {
            if (node.MutationType == SqlNodeType.None) continue;

            var upserts = string.Join(", ", node.UpsertKeys.Select(u => $"{u.Entity}.{u.Column}"));
            sb.AppendLine($"MERGE INTO {node.EntityName} USING ... ON ({upserts})");
        }

        return sb.ToString();
    }
}