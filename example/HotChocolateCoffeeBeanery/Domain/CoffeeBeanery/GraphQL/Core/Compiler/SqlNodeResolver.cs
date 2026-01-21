using System;
using System.Linq;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SqlNodeResolver
    {
        public static (Dictionary<string, SqlNode> select, Dictionary<string, SqlNode> edge, Dictionary<string, SqlNode> mutation)
            ResolveFromSelection<TModel>(
                ISelection rootSelection,
                string rootEntity,
                bool isMutation = false)
            where TModel : class
        {
            var (select, edge, mutation) = SqlNodeBuilder.BuildFromModel<TModel>();

            VisitSelectionTree(rootSelection.SyntaxNode, rootEntity, select, edge, mutation, isMutation);

            return (select, edge, mutation);
        }

        private static void VisitSelectionTree(
            ISyntaxNode node,
            string currentEntity,
            Dictionary<string, SqlNode> select,
            Dictionary<string, SqlNode> edge,
            Dictionary<string, SqlNode> mutation,
            bool isMutation)
        {
            var name = node.ToString().Split('{')[0].Trim();
            ApplyNode(currentEntity, name, select, edge, mutation, isMutation);

            foreach (var child in node.GetNodes())
            {
                string fieldName = child.ToString().Split('{')[0].Trim();

                if (select.Keys.Any(k => k.StartsWith($"{currentEntity}~{fieldName}")))
                {
                    VisitSelectionTree(child, fieldName, select, edge, mutation, isMutation);
                }
                else if (IsGraphField(fieldName))
                {
                    VisitSelectionTree(child, fieldName, select, edge, mutation, isMutation);
                }
                else
                {
                    VisitSelectionTree(child, currentEntity, select, edge, mutation, isMutation);
                }
            }
        }

        private static void ApplyNode(
            string entity,
            string field,
            Dictionary<string, SqlNode> select,
            Dictionary<string, SqlNode> edge,
            Dictionary<string, SqlNode> mutation,
            bool isMutation)
        {
            var key = $"{entity}~{field}";

            if (select.TryGetValue(key, out var node))
            {
                node.SqlNodeType = isMutation ? SqlNodeType.Mutation : SqlNodeType.Select;
            }
        }

        private static bool IsGraphField(string field) =>
            field.EndsWith("Edge", StringComparison.OrdinalIgnoreCase) ||
            field.Contains("Graph", StringComparison.OrdinalIgnoreCase);
    }
}
