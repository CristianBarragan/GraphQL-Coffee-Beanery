namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public class SqlStructure
    {
        public string Sql { get; set; }
        
        public IReadOnlyList<string> EntityOrder { get; init; }
    }
}