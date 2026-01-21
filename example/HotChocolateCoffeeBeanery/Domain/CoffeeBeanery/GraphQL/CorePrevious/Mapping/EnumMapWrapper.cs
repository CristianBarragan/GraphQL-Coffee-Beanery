namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class EnumMapWrapper
    {
        public System.Type ModelType { get; init; }
        public System.Type EntityType { get; init; }
        public System.Collections.IDictionary ToEntityMap { get; init; }
        public System.Collections.IDictionary ToModelMap { get; init; }
    }
}