using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.GraphQL
{
    public sealed class NodeTree
    {
        
        public string Alias { get; init; } = "";
        public string Name { get; init; } = "";
        public string Schema { get; set; } = "public";
        public int Id { get; init; }

        public NodeMap NodeMap { get; set; }

        public List<LinkKey> Parents { get; set; } = new();
        
        public List<LinkKey> RelatedParents { get; set; } = new();
        
        public List<LinkKey> RelatedChildren { get; set; } = new();
        
        public List<LinkKey> Children { get; set; } = new();

        public List<LinkKey> ModelToEntityLinks { get; set; } = new();

        public List<FieldMap> Mapping { get; set; } = new();
        
        public List<string> UpsertKeys { get; set; } = new();

        public bool IsGraph { get; set; }

        public bool IsEntity { get; set; }

        public bool IsModel { get; set; }
        
    }
}