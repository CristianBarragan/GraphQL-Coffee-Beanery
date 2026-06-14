using System.Collections.Concurrent;
using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Runtime;

namespace CoffeeBeanery.GraphQL.Core.Warmup;

public static class GraphRuntime
{
    public static readonly ConcurrentDictionary<Type, object> Hydrators = new();

    public static void Warmup(GraphIL graph)
    {
        var queryPlanner = new QueryPlanner();

        foreach (var node in graph.Nodes.Values)
        {
            var plan = queryPlanner.Build(graph, node.Alias);

            var columns = plan.Nodes.Values
                .SelectMany(n => n.Columns.Select((c, i) => new SelectColumn
                {
                    Alias = n.Alias,
                    Property = c,
                    Index = i
                }))
                .ToList();

            Hydrators[node.EntityType] =
                BuildHydrator(node.EntityType, columns);
        }
    }

    private static object BuildHydrator(Type type, List<SelectColumn> cols)
    {
        var method = typeof(HydrationCompiler)
            .GetMethods()
            .Single(m => m.Name == "Build" && m.IsGenericMethodDefinition)
            .MakeGenericMethod(type);

        return method.Invoke(null, new object[] { cols })!;
    }
}