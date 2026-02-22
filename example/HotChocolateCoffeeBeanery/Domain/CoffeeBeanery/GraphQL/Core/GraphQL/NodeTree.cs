using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.GraphQL
{
    public sealed class NodeTree
    {
        public string Name { get; init; } = "";
        public string Schema { get; init; } = "public";
        public string Id { get; init; }

        public string ParentName { get; set; } = "";
        public List<string> Children { get; set; } = new();

        public List<FieldMap> Mapping { get; set; } = new();

        public bool IsGraph { get; set; }
    }
}