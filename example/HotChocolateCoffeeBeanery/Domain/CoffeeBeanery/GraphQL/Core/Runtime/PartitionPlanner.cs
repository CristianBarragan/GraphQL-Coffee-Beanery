// using CoffeeBeanery.GraphQL.Core.Contracts;
//
// namespace CoffeeBeanery.GraphQL.Core.Runtime;
//
// public static class PartitionPlanner
// {
//     public static List<ExecutionPartition> Build(QueryPlan plan)
//     {
//         var partitions = new List<ExecutionPartition>();
//         var visited = new HashSet<string>();
//
//         foreach (var node in plan.Nodes.Values)
//         {
//             if (visited.Contains(node.Alias))
//                 continue;
//
//             var partition = new ExecutionPartition
//             {
//                 Key = node.Alias
//             };
//
//             BuildPartition(node, plan, partition, visited);
//
//             partition.EstimatedCost = partition.Nodes.Count * 10;
//
//             partition.CanExecuteInParallel = partition.Nodes.Count > 1;
//
//             partitions.Add(partition);
//         }
//
//         return partitions;
//     }
//
//     private static void BuildPartition(
//         PlanNode node,
//         QueryPlan plan,
//         ExecutionPartition partition,
//         HashSet<string> visited)
//     {
//         if (!visited.Add(node.Alias))
//             return;
//
//         partition.Nodes.Add(node);
//
//         foreach (var join in node.Joins)
//         {
//             if (!plan.Nodes.TryGetValue(join.ToAlias, out var next))
//                 continue;
//
//             BuildPartition(next, plan, partition, visited);
//         }
//     }
// }