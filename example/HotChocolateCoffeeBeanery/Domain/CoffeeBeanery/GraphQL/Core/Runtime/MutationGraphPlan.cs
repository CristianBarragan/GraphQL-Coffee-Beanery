namespace CoffeeBeanery.GraphQL.Core.Runtime;

public sealed class MutationGraphPlan
{
    public Dictionary<string, MutationNodePlan> Nodes { get; init; }
        = new(StringComparer.OrdinalIgnoreCase);

    public List<MutationEdgePlan> Edges { get; init; }
        = new();

    public MutationNodePlan Root { get; init; } = default!;
}

public sealed class MutationNodePlan
{
    public string Alias { get; init; } = "";
    public string Table { get; init; } = "";

    public string Schema { get; init; } = "public";

    public List<MutationFieldPlan> Fields { get; init; } = new();

    public List<MutationEdgePlan> OutgoingEdges { get; init; } = new();

    public Type EntityType { get; init; } = default!;
}

public sealed record MutationFieldPlan(
    string Column,
    object? Value
);

public sealed class MutationEdgePlan
{
    public string FromAlias { get; init; } = "";
    public string ToAlias { get; init; } = "";

    public string FromColumn { get; init; } = "";
    public string ToColumn { get; init; } = "";
}