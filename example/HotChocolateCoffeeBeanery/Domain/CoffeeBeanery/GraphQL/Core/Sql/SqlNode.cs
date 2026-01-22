namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public enum SqlNodeType
    {
        Node,
        Edge,
        Query,
        Mutation,
        Graph
    }

    public sealed class SqlNode
    {
        public string Entity { get; set; } = "";
        public string Schema { get; set; } = "public";
        public string Table { get; set; } = "";
        public string Column { get; set; } = "";
        public string Value { get; set; } = "";
        public List<string> UpsertKeys { get; set; } = new();
        public List<LinkKey> LinkKeys { get; set; } = new();
        public string Graph { get; set; } = "";
        public bool IsColumnGraph { get; set; } = false;


        public SqlNodeType SqlNodeType { get; set; }

        // Used for joins
        public string JoinTable { get; set; } = "";
        public string JoinColumnFrom { get; set; } = "";
        public string JoinColumnTo { get; set; } = "";

        // For edges
        public string Relationship { get; set; } = "";
        public string RelationshipKey { get; set; } = "";

        // For enum mapping
        public Dictionary<string, string> FromEnumeration { get; set; } = new();
        public Dictionary<string, string> ToEnumeration { get; set; } = new();

        public bool IsGraph => SqlNodeType == SqlNodeType.Graph;
        public bool IsEdge => SqlNodeType == SqlNodeType.Edge;

    }
}