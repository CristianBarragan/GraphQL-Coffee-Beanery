// using CoffeeBeanery.GraphQL.Core.Contracts;
//
// namespace CoffeeBeanery.GraphQL.Core.Runtime;
//
// public class TraceDrivenOptimizer : IQueryOptimizer
// {
//     private readonly QueryTraceCollector _traces;
//
//     public TraceDrivenOptimizer(QueryTraceCollector traces)
//     {
//         _traces = traces;
//     }
//
//     public QueryPlan Optimize(QueryPlan plan, OptimizationContext context)
//     {
//         var traceStats = _traces.Get(context.ShapeKey).ToList();
//
//         if (!traceStats.Any())
//             return plan;
//
//         var avgExec = traceStats.Average(x => x.ExecutionTimeMs);
//         var cacheMissRate = 1 - traceStats.Count(x => x.CacheHit) /
//             (double)traceStats.Count;
//
//         // ----------------------------------------------------
//         // RULE 1: too slow → reduce join complexity
//         // ----------------------------------------------------
//         if (avgExec > 400)
//         {
//             foreach (var node in plan.Nodes.Values)
//             {
//                 node.Joins = node.Joins.Take(node.Joins.Count / 2).ToList();
//             }
//         }
//
//         // ----------------------------------------------------
//         // RULE 2: cache ineffective → simplify plan structure
//         // ----------------------------------------------------
//         if (cacheMissRate > 0.5)
//         {
//             plan.EnableSubtreeCaching = true;
//         }
//
//         return plan;
//     }
// }