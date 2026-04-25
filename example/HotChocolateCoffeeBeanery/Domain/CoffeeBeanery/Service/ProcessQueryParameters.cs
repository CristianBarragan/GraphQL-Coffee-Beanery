using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.Service
{
    public sealed class ProcessQueryParameters
    {
        public SqlStructure SqlStructure { get; set; } = new();
        public Pagination Pagination { get; set; } = new();

        public string Model { get; set; }
    }
}