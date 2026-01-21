using System;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Configuration
{
    public class Map
    {
        public Type ModelType { get; set; }
        public Type EntityType { get; set; }
        public UpsertKey[] EntityUpsertKeys { get; set; }
        public LinkKey[] JoinKeys { get; set; }
        public object[] PropertyMappings { get; set; }
    }
}