namespace CoffeeBeanery.GraphQL.Core.Compiler;

public sealed class CompilationResult
{
    public string Sql { get; init; }
    public IReadOnlyList<string> EntityOrder { get; init; }
    public IReadOnlyDictionary<string, Type> SplitOn { get; init; }
}
