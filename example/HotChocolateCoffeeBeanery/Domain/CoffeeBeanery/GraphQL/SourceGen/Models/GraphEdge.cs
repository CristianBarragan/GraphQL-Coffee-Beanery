namespace CoffeeBeanery.GraphQL.SourceGen.Models
{
    public sealed class GraphEdge
    {
        public string Key { get; set; } = "";
        public string Entity { get; set; } = "";
        public string Column { get; set; } = "";
        public bool IsGraph { get; set; }
        public string RelationshipKey { get; set; } = "";
    }
}