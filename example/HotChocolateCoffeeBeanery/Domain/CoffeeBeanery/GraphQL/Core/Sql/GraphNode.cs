using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

public sealed class GraphNode
{
    public string Id { get; set; }

    public NodeMap Map { get; set; }

    public string Alias { get; set; }
    
    public string Name { get; set; }

    public Type? ModelType => Map.ModelType;

    public Type? EntityType => Map.EntityType;

    public bool IsModel => Map.IsModel;

    public bool IsEntity => Map.IsEntity;

    public List<FieldMap> Fields { get; } = new();

    public List<GraphEdge> Edges { get; } = new();
}