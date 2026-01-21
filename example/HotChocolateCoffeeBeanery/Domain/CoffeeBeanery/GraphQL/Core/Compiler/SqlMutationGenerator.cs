using System;
using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SqlMutationGenerator
    {
        public static string BuildMutation(
            SqlCompilationContext ctx,
            Dictionary<string, SqlNode> nodes,
            NodeTree root)
        {
            // This builds a simple INSERT statement
            // based on resolved nodes.

            var rootNode = nodes.Values.FirstOrDefault(n => n.SqlNodeType == SqlNodeType.Mutation);
            if (rootNode == null)
                throw new InvalidOperationException("No mutation node found.");

            var columns = nodes.Values
                .Where(n => n.SqlNodeType == SqlNodeType.Mutation && n.EntityName == root.Name)
                .Select(n => n.ColumnName)
                .Distinct()
                .ToList();

            var sqlColumns = string.Join(", ", columns.Select(c => $"\"{c}\""));
            var sqlValues = string.Join(", ", columns.Select(c => $"@{c}"));

            return $"INSERT INTO \"{root.Name}\" ({sqlColumns}) VALUES ({sqlValues});";
        }
    }
}