// SqlMutationCompiler.cs
using CoffeeBeanery.GraphQL.Core.Runtime;
using HotChocolate.Language;
using HotChocolateCoffeeBeanery.GraphQL.Core.Runtime;

public static class SqlMutationCompiler
{
    // SqlMutationCompiler.cs — add debug output to verify extraction
    public static (string InsertSql, string GraphMergeSql) Compile(
        GraphIL     graph,
        ISyntaxNode wrapperArgValue)
    {
        var mutationValues = MutationValueExtractor.Extract(graph, wrapperArgValue);

        // DEBUG — remove after confirming
        Console.WriteLine("=== MutationValues extracted ===");
        foreach (var (alias, fields) in mutationValues)
        {
            Console.WriteLine($"  [{alias}]");
            foreach (var (k, v) in fields)
                Console.WriteLine($"    {k} = {v}");
        }

        var plan = MutationPlanner.Build(graph, mutationValues);

        Console.WriteLine("=== MutationPlan nodes ===");
        foreach (var node in plan.Nodes.Values)
        {
            Console.WriteLine($"  [{node.Alias}] identity=[{string.Join(",", node.IdentityFields.Select(f => f.Column))}] data=[{string.Join(",", node.DataFields.Select(f => f.Column))}]");
        }

        var (inserts, merges) = MutationRenderer.Render(plan, graph);

        Console.WriteLine("=== Inserts ===");
        foreach (var s in inserts) Console.WriteLine(s);
        Console.WriteLine("=== Merges ===");
        foreach (var s in merges) Console.WriteLine(s);

        return (
            string.Join(";\n", inserts),
            string.Join("\n",  merges)
        );
    }
}