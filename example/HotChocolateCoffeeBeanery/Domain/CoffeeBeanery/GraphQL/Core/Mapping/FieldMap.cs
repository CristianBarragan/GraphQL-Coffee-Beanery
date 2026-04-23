
namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class FieldMap
    {
        public string SourceModel { get; set; } = "";
        public string SourceName { get; set; } = "";
        public string DestinationEntity { get; set; } = "";
        public string DestinationName { get; set; } = "";
        
    }
}