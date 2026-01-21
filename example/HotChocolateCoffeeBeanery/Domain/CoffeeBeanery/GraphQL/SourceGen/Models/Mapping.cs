namespace CoffeeBeanery.GraphQL.SourceGen.Models
{
    public sealed class Mapping
    {
        public string Name { get; set; } = "";
        public string Model { get; set; } = "";
        public string Entity { get; set; } = "";
        public string ModelProp { get; set; } = "";
        public string EntityProp { get; set; } = "";
    }
}