using System;
using System.Collections.Generic;
using System.Linq;
using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.CoreNew.Sql;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.CoreNew.Compiler
{
    public static class SqlNodeResolverNew
    {
        public static Dictionary<string, SqlNode> ResolveFromSelection<TModel>(
            ISelection rootSelection,
            string rootEntityName,
            bool isMutation = false)
            where TModel : class
        {
            var nodes = SqlNodeBuilder.BuildFromModel<TModel>();
            WalkSelection(rootSelection, rootEntityName, nodes, isMutation);
            return nodes;
        }

        private static void WalkSelection(
            ISelection sel,
            string currentEntity,
            Dictionary<string, SqlNode> nodes,
            bool isMutation)
        {
            foreach (var field in sel.Fields)
            {
                MarkField(currentEntity, field.Field.Name.Value, nodes, isMutation);
            }

            foreach (var child in sel.SelectionSet ?? Enumerable.Empty<ISelection>())
            {
                var nextEntity = child.Field.Name.Value;
                if (nodes.Keys.Any(k => k.StartsWith($"{currentEntity}~{nextEntity}")))
                {
                    WalkSelection(child, nextEntity, nodes, isMutation);
                }
                else if (IsGraphField(child))
                {
                    MarkGraph(currentEntity, nextEntity, nodes);
                    WalkSelection(child, nextEntity, nodes, isMutation);
                }
            }
        }

        private static void MarkField(
            string entity,
            string field,
            Dictionary<string, SqlNode> nodes,
            bool isMutation)
        {
            var key = $"{entity}~{field}";
            if (nodes.TryGetValue(key, out var n))
            {
                n.SqlNodeType = isMutation ? SqlNodeType.Mutation : SqlNodeType.Select;
            }
        }

        private static bool IsGraphField(ISelection sel) =>
            sel.Field.Name.Value.EndsWith("Edge", StringComparison.OrdinalIgnoreCase)
            || sel.Field.Name.Value.Contains("Graph", StringComparison.OrdinalIgnoreCase);

        private static void MarkGraph(
            string entity,
            string graphName,
            Dictionary<string, SqlNode> nodes)
        {
            var key = $"{entity}~{graphName}";
            if (nodes.TryGetValue(key, out var n))
            {
                n.SqlNodeType = SqlNodeType.Graph;
            }
        }
    }
}
