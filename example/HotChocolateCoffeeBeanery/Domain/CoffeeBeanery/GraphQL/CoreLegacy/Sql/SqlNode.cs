using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public class SqlNode
    {
        public string EntityName { get; init; }
        public string PropertyName { get; init; }

        // Converted from LinkMaps
        public List<LinkKey> LinkKeys { get; } = new();
        public List<UpsertKey> UpsertKeys { get; } = new();
        public List<IEnumMap> EnumMaps { get; } = new();

        public object Value { get; set; }
        public SqlNodeType SqlNodeType { get; set; }
    }

    public enum SqlNodeType
    {
        None,
        Mutation,
        Insert,
        Update,
        Delete
    }
}