using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public sealed class SqlNode
    {
        public string EntityName { get; init; } = "";
        public string ColumnName { get; init; } = "";
        public SqlNodeType SqlNodeType { get; set; }

        public List<LinkKey> LinkKeys { get; } = new();
    }

    public enum SqlNodeType
    {
        Select,
        Graph,
        Edge,
        Mutation
    }
}