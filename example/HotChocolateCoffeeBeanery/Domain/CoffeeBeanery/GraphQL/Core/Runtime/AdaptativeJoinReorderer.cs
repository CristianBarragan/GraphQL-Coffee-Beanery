// using CoffeeBeanery.GraphQL.Core.Contracts;
//
// namespace CoffeeBeanery.GraphQL.Core.Runtime;
//
// public static class AdaptiveJoinReorderer
// {
//     public static void Apply(QueryPlan plan, ExecutionHistory history, string key)
//     {
//         var stats = history.Get(key).ToList();
//         if (!stats.Any()) return;
//
//         var slowJoins = stats
//             .Where(x => x.JoinCount > 10 && x.ExecutionTimeMs > 300)
//             .Any();
//
//         if (!slowJoins) return;
//
//         foreach (var node in plan.Nodes.Values)
//         {
//             node.Joins = node.Joins
//                 .OrderBy(j => j.ToAlias.Length) // cheap heuristic fallback
//                 .ToList();
//         }
//     }
// }