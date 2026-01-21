namespace CoffeeBeanery.GraphQL.Core.Mapping;

public sealed class MutationNode
{
    public string Entity { get; init; } = "";
    public string Column { get; init; } = "";
    public string Parameter { get; init; } = "";
}
