using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlTreeWalker
    {
        public static void WalkQueryNode(
            NodeTree root,
            Dictionary<string, NodeTree> nodeEntities,
            Dictionary<string, SqlNode> edgeDict,
            Dictionary<string, SqlNode> nodeDict,
            SqlCompilationContext ctx)
        {
            // root fields
            AddNodeQueryColumns(root, nodeDict, ctx);

            // traverse
            TraverseQueryNode(root, nodeEntities, nodeDict, edgeDict, ctx);
        }

        private static void TraverseQueryNode(
            NodeTree node,
            Dictionary<string, NodeTree> nodeEntities,
            Dictionary<string, SqlNode> nodeDict,
            Dictionary<string, SqlNode> edgeDict,
            SqlCompilationContext ctx)
        {
            foreach (var childName in node.Children)
            {
                var child = nodeEntities[childName];
                
                // Edge join (INNER)
                if (edgeDict.TryGetValue($"{node.Name}~{child.Name}", out var edge))
                {
                    ctx.SelectSqlFields.Add($"{edge.RelationshipKey}.\"{edge.JoinColumnTo}\" AS \"{child.Name}_{edge.JoinColumnTo}\"");
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

                AddNodeQueryColumns(child, nodeDict, ctx);

                TraverseQueryNode(child, nodeEntities, nodeDict, edgeDict, ctx);
            }
        }

        private static void AddNodeQueryColumns(NodeTree node, Dictionary<string, SqlNode> nodeDict, SqlCompilationContext ctx)
        {
            foreach (var field in node.Mapping)
            {
                if (nodeDict.TryGetValue($"{node.Name}~{field.SourceName}", out var sqlNode))
                {
                    ctx.SelectSqlFields.Add($"{node.Name}.\"{sqlNode.Column}\" AS \"{node.Name}_{sqlNode.Column}\"");
                }
            }
        }
        
        public static void WalkMutationNode(
            NodeTree root,
            Dictionary<string, NodeTree> nodeEntities,
            Dictionary<string, SqlNode> mutationDict,
            SqlCompilationContext ctx)
        {
            // root fields
            AddMutationColumns(root, mutationDict, ctx);

            // traverse
            TraverseMutationNode(root, nodeEntities, mutationDict, ctx);
        }

        private static void TraverseMutationNode(
            NodeTree node,
            Dictionary<string, NodeTree> nodeEntities,
            Dictionary<string, SqlNode> WalkMutation,
            SqlCompilationContext ctx)
        {
            foreach (var childName in node.Children)
            {
                var child = nodeEntities[childName];
                // // Edge join (INNER)
                // if (edgeDict.TryGetValue($"{node.Name}~{child.Name}", out var edge))
                // {
                //     ctx.SelectSqlFields.Add($"{edge.Relationship}.\"{edge.JoinColumnTo}\" AS \"{child.Name}_{edge.JoinColumnTo}\"");
                //     ctx.SelectSqlFields.Add($"{node.Name}.\"{edge.JoinColumnFrom}\" AS \"{node.Name}_{edge.JoinColumnFrom}\"");
                // }
                //
                // // Node join (LEFT)
                // if (nodeDict.TryGetValue($"{node.Name}~{child.Name}", out var join))
                // {
                //     ctx.SelectSqlFields.Add($"{child.Name}.\"{join.JoinColumnTo}\" AS \"{child.Name}_{join.JoinColumnTo}\"");
                // }

                //TODO implemente mutation
                
                // Graph node
                if (child.Mapping.Any() && child.Mapping.Any(m => m.DestinationName == "Graph"))
                {
                    // Placeholder
                    ctx.SelectSqlFields.Add($"-- GRAPH: {child.Name}");
                }

                AddMutationColumns(child, WalkMutation, ctx);

                TraverseMutationNode(child, nodeEntities, WalkMutation, ctx);
            }
        }

        private static void AddMutationColumns(NodeTree node, Dictionary<string, SqlNode> nodeDict, SqlCompilationContext ctx)
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
