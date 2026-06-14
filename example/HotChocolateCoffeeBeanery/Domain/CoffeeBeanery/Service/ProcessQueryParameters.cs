using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.Service
{
    public sealed class ProcessQueryParameters
    {
        public HydrationRuntimeContext Context { get; set; } = new();
        public string Model { get; set; }
        public GraphIL Graph { get; set; }
    }
}