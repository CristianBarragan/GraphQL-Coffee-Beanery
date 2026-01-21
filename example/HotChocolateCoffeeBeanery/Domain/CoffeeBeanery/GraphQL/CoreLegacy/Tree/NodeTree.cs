using CoffeeBeanery.GraphQL.Core.Mapping;

public sealed class NodeTree
{
    public string Name { get; init; } = "";
    public string Schema { get; init; } = "public";
    public int Id { get; init; }  // now int

    public string ParentName { get; set; } = "";

    public List<NodeTree> Children { get; } = new();
    public List<string> ChildrenName => Children.Select(c => c.Name).ToList();

    public List<FieldMap> Mapping { get; } = new();
}