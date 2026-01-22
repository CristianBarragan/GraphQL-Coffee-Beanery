using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class EntityMap
    {
        public string Schema { get; set; } = "public";

        public List<FieldMap> FieldMaps { get; } = new();
        public List<UpsertKey> UpsertKeys { get; } = new();

        // ENUMS
        public Dictionary<string, string> FromEnum { get; set; } = new();
        public Dictionary<string, string> ToEnum { get; set; } = new();
    }

}