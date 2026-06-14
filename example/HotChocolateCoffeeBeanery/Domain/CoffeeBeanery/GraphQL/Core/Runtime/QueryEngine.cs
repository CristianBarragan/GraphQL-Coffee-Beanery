// using CoffeeBeanery.GraphQL.Core.Contracts;
// using CoffeeBeanery.GraphQL.Core.GraphQL;
//
// namespace CoffeeBeanery.GraphQL.Core.Runtime;
//
// public class QueryEngine
// {
//     private readonly QueryPlanner _planner = new();
//
//     public async Task<List<T>> ExecuteAsync<T>(
//         PlanningContext context,
//         Func<string, Task<List<object[]>>> executor,
//         Func<object[], T> hydrator)
//     {
//         var plan = _planner.Build(context);
//         var sql = SqlRenderer.Render(plan);
//
//         var rows = await executor(sql);
//
//         return rows.Select(hydrator).ToList();
//     }
// }