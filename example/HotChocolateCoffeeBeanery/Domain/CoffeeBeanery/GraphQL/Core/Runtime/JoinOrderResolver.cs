using CoffeeBeanery.GraphQL.Core.Contracts;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class JoinOrderResolver
{
    public static List<PlanJoin> Order(QueryPlan plan, string rootAlias)
    {
        var result = new List<PlanJoin>();

        // adjacency list: FromAlias -> joins
        var lookup = plan.Joins
            .GroupBy(j => j.FromAlias)
            .ToDictionary(
                g => g.Key,
                g => g.ToList(),
                StringComparer.OrdinalIgnoreCase);

        var visitedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var visitedJoins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var stack = new Stack<string>();
        stack.Push(rootAlias);
        visitedAliases.Add(rootAlias);

        while (stack.Count > 0)
        {
            var currentAlias = stack.Pop();

            if (!lookup.TryGetValue(currentAlias, out var outgoingJoins))
                continue;

            foreach (var join in outgoingJoins)
            {
                // avoid duplicate joins (important in cyclic graphs / multi-path graphs)
                var joinKey = $"{join.FromAlias}->{join.ToAlias}->{join.FromColumn}->{join.ToColumn}";
                if (!visitedJoins.Add(joinKey))
                    continue;

                result.Add(join);

                // visit next node only once
                if (visitedAliases.Add(join.ToAlias))
                {
                    stack.Push(join.ToAlias);
                }
            }
        }

        return result;
    }
}