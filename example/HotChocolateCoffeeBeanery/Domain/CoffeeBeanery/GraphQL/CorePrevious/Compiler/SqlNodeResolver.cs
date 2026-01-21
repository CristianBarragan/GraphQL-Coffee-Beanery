using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SqlNodeResolver
    {
        public static Dictionary<string, SqlNode> ResolveFromSelection(
            ISelection selection,
            NodeTree root,
            bool isMutation)
        {
            var nodes = SqlNodeBuilder.BuildFromNodeTree(root);
            Visit(selection.SyntaxNode, root.Name, nodes, isMutation);
            return nodes;
        }

        private static void Visit(
            ISyntaxNode node,
            string entity,
            Dictionary<string, SqlNode> nodes,
            bool isMutation)
        {
            var field = node.ToString().Split('{')[0].Trim();
            var key = $"{entity}~{field}";

            if (nodes.TryGetValue(key, out var n))
            {
                n.SqlNodeType = isMutation ? SqlNodeType.Mutation : SqlNodeType.Select;
            }

            foreach (var child in node.GetNodes())
                Visit(child, entity, nodes, isMutation);
        }
    }
}