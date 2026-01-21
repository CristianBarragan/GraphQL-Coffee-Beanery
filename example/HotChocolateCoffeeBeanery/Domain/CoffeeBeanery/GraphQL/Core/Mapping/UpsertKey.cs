namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class UpsertKey
    {
        public string SourceKey { get; init; }
        public string DestinationKey { get; init; }

        public UpsertKey(string sourceKey, string destinationKey)
        {
            SourceKey = sourceKey;
            DestinationKey = destinationKey;
        }
    }
}