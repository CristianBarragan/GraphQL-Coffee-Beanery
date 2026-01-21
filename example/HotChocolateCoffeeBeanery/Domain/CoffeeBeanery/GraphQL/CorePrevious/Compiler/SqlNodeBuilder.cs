using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SqlNodeBuilder
    {
        public static Dictionary<string, SqlNode> BuildFromNodeTree(NodeTree root)
        {
            var nodes = new Dictionary<string, SqlNode>();
            Walk(root, nodes);
            return nodes;
        }

        private static void Walk(NodeTree node, Dictionary<string, SqlNode> nodes)
        {
            var idKey = $"{node.Name}~Id";
            if (!nodes.ContainsKey(idKey))
            {
                nodes[idKey] = new SqlNode
                {
                    EntityName = node.Name,
                    ColumnName = "Id",
                    SqlNodeType = SqlNodeType.Select
                };
            }

            foreach (var child in node.Children)
            {
                var link = new LinkKey
                {
                    From = $"{node.Name}~Id",
                    To = $"{child.Name}~Id"
                };

                nodes[idKey].LinkKeys.Add(link);
                Walk(child, nodes);
            }
        }
    }
}