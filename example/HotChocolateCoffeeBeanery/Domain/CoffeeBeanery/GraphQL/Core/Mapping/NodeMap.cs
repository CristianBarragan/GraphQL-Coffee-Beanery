using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class NodeMap
    {
        public string Schema { get; set; } = "public";

        public bool IsModel { get; set; }
        
        public bool IsEntity { get; set; }

        public List<FieldMap> FieldMaps { get; } = new();
        public List<UpsertKey> UpsertKeys { get; private set; } = new();
        public List<LinkKey> LinkKeys { get; private set; } = new();

        // ENUMS
        public Dictionary<string, string> FromEnum { get; set; } = new();
        public Dictionary<string, string> ToEnum { get; set; } = new();
        public List<string> Children { get; private set; } = new();
        
        public bool IsGraph { get; set; }
    }

}