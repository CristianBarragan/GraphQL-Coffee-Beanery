using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlTreeWalker
    {
        public static void Walk(
            NodeTree root,
            Dictionary<string, SqlNode> mutationDict,
            Dictionary<string, SqlNode> edgeDict,
            Dictionary<string, SqlNode> nodeDict,
            SqlCompilationContext ctx)
        {
            // root fields
            AddNodeColumns(root, nodeDict, ctx);

            // traverse
            WalkNode(root, nodeDict, edgeDict, ctx);
        }

        private static void WalkNode(
            NodeTree node,
            Dictionary<string, SqlNode> nodeDict,
            Dictionary<string, SqlNode> edgeDict,
            SqlCompilationContext ctx)
        {
            foreach (var child in node.Children)
            {
                // Edge join (INNER)
                if (edgeDict.TryGetValue($"{node.Name}~{child.Name}", out var edge))
                {
                    ctx.SelectSqlFields.Add($"{edge.Relationship}.\"{edge.JoinColumnTo}\" AS \"{child.Name}_{edge.JoinColumnTo}\"");
                    ctx.SelectSqlFields.Add($"{node.Name}.\"{edge.JoinColumnFrom}\" AS \"{node.Name}_{edge.JoinColumnFrom}\"");
                }

                // Node join (LEFT)
                if (nodeDict.TryGetValue($"{node.Name}~{child.Name}", out var join))
                {
                    ctx.SelectSqlFields.Add($"{child.Name}.\"{join.JoinColumnTo}\" AS \"{child.Name}_{join.JoinColumnTo}\"");
                }

                // Graph node
                if (child.Mapping.Any() && child.Mapping.Any(m => m.DestinationName == "Graph"))
                {
                    // Placeholder
                    ctx.SelectSqlFields.Add($"-- GRAPH: {child.Name}");
                }

                AddNodeColumns(child, nodeDict, ctx);

                WalkNode(child, nodeDict, edgeDict, ctx);
            }
        }

        private static void AddNodeColumns(NodeTree node, Dictionary<string, SqlNode> nodeDict, SqlCompilationContext ctx)
        {
            foreach (var field in node.Mapping)
            {
                if (nodeDict.TryGetValue($"{node.Name}~{field.SourceName}", out var sqlNode))
                {
                    ctx.SelectSqlFields.Add($"{node.Name}.\"{sqlNode.Column}\" AS \"{node.Name}_{sqlNode.Column}\"");
                }
            }
        }
    }
}
