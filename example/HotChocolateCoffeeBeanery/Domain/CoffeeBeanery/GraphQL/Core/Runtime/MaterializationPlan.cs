using CoffeeBeanery.GraphQL.Core.Contracts;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public sealed class MaterializationPlan
{
    public Type RootType { get; init; }

    public List<SelectColumn> Columns { get; } = new();

    public Dictionary<string, GraphNodePlan> Nodes { get; } = new();
}