namespace CoffeeBeanery.GraphQL.Core.Sql;

public sealed class JoinEdge
{
    public string FromAlias { get; init; }
    public string ToAlias { get; init; }

    public string FromColumn { get; init; }
    public string ToColumn { get; init; }
}