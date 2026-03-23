using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.GraphQL
{
    public sealed class NodeTree
    {
        
        public string Alias { get; init; } = "";
        public string Name { get; init; } = "";
        public string Schema { get; set; } = "public";
        public int Id { get; init; }

        public string ParentName { get; set; } = "";
        public List<string> Children { get; set; } = new();

        public List<FieldMap> Mapping { get; set; } = new();
        
        public List<string> UpsertKeys { get; set; } = new();

        public bool IsGraph { get; set; }
        
    }
}