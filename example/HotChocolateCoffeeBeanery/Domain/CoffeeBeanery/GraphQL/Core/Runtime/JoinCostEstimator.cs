//
// using CoffeeBeanery.GraphQL.Core.GraphQL;
//
// namespace CoffeeBeanery.GraphQL.Core.Runtime;
//
// public static class JoinCostEstimator
// {
//     // very simple heuristic (we can evolve later)
//     public static int Estimate(NodeTree node)
//     {
//         var size = node.Children.Count + node.RelatedChildren.Count;
//
//         // deeper = more expensive
//         var depthPenalty = GetDepth(node) * 10;
//
//         return size + depthPenalty;
//     }
//
//     private static int GetDepth(NodeTree node)
//     {
//         int depth = 0;
//
//         var current = node.Parent;
//
//         while (current != null)
//         {
//             depth++;
//             current = current.Parent;
//         }
//
//         return depth;
//     }
// }