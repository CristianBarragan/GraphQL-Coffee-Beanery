using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public enum SqlNodeType
    {
        Select,
        Edge,
        Mutation
    }

    public sealed class LinkKey
    {
        public string From { get; set; } = "";
        public string To { get; set; } = "";
        public string FromId { get; set; } = "";
        public string ToId { get; set; } = "";
    }

    public sealed class SqlNode
    {
        public string EntityName { get; set; } = "";
        public string ColumnName { get; set; } = "";
        public string Schema { get; set; } = "public";

        public Type EntityType { get; set; } = default!;

        public SqlNodeType SqlNodeType { get; set; }
        public List<LinkKey> LinkKeys { get; set; } = new();
    }

}