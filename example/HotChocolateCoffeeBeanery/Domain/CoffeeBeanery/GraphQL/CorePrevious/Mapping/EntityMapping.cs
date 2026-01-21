namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class EntityMapping
    {
        public string Entity { get; init; } = "";
        public string Schema { get; init; } = "public";

        public Dictionary<string, string> Fields { get; } = new();

        // JOIN info
        public string ParentKey { get; init; } = "id";
        public string ChildKey { get; init; } = "id";
        public bool IsEdge { get; init; }
    }
}
