using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public sealed class GraphIL
{
    public required string SchemaVersion { get; init; }

    public required Dictionary<string, GraphILNode> Nodes { get; init; }

    public required Dictionary<string, List<GraphILEdge>> EdgesBySourceAlias { get; init; }

    public required Dictionary<string, List<GraphILEdge>> EdgesByTargetAlias { get; init; }

    public string Sql { get; set; }
}

public sealed class GraphILNode
{
    public string Alias      { get; set; } = "";
    public string TableName  { get; set; }
    public Type   EntityType { get; set; } = default!;
    public List<GraphILField> Fields   { get; set; }
    public List<string>       Columns  { get; set; } = new();
    public List<string>       Children { get; set; } = new();
    public List<string>       RelatedChildren { get; set; } = new();
    public string             Schema   { get; set; }
    public List<string>       UpsertKeys { get; set; } = new();
    public GraphMap            GraphMap { get; set; }
    public bool                IsModel  { get; set; }   // ← add
    public bool                IsEntity { get; set; }   // ← add
}

public sealed class GraphILEdge
{
    public string FromAlias { get; set; } = "";
    public string ToAlias { get; set; } = "";
    public string FromColumn { get; set; } = "";
    public string ToColumn { get; set; } = "";
}

public sealed class GraphILField
{
    public string SourceName { get; init; } = "";
    public string DestinationName { get; init; } = "";

    public Dictionary<string, int> Enumerations { get; set; }
}