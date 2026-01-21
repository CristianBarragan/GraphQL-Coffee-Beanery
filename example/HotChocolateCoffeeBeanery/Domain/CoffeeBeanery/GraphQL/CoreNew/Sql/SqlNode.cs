namespace CoffeeBeanery.GraphQL.CoreNew.Sql
{
    public enum SqlNodeType
    {
        None,
        Select,
        Mutation,
        Edge,
        Graph
    }

    public sealed class SqlNode
    {
        public string Key { get; init; } = null!;
        public string EntityName { get; init; } = null!;
        public string ColumnName { get; init; } = null!;
        public SqlNodeType SqlNodeType { get; set; } = SqlNodeType.None;
        public object? Value { get; set; }
        public string? Cypher { get; set; }

        public List<LinkKey> LinkKeys { get; } = new();
        public List<UpsertKey> UpsertKeys { get; } = new();
    }
}