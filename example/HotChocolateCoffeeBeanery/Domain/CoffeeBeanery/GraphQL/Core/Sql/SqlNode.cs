namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public enum SqlNodeType
    {
        Node,
        Edge,
        Mutation,
        Graph
    }

    public sealed class SqlNode : ICloneable
    {
        public string Id { get; set; }
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
        public string RelationshipKey { get; set; } = "";

        // For enum mapping
        public Dictionary<string, string> FromEnumeration { get; set; } = new();
        public Dictionary<string, string> ToEnumeration { get; set; } = new();

        public bool IsGraph => SqlNodeType == SqlNodeType.Graph;
        public bool IsEdge => SqlNodeType == SqlNodeType.Edge;
        public bool IsNode => SqlNodeType == SqlNodeType.Node;
        
        public object Clone()
        {
            return new SqlNode()
            {
                Schema = this.Schema,
                Table = this.Table,
                Column = this.Column,
                Value = this.Value,
                UpsertKeys = this.UpsertKeys,
                LinkKeys = this.LinkKeys,
                Graph = this.Graph,
                IsColumnGraph = this.IsColumnGraph,
                SqlNodeType = this.SqlNodeType,
                RelationshipKey = this.RelationshipKey,
                FromEnumeration = this.FromEnumeration,
                ToEnumeration = this.ToEnumeration
            };
        }
    }
}