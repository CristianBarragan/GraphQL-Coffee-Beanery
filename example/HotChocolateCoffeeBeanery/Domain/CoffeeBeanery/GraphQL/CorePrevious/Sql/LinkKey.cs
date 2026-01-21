namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public sealed class LinkKey
    {
        public string From { get; init; } = null!;
        public string FromId { get; init; } = null!;
        public string To { get; init; } = null!;
        public string ToId { get; init; } = null!;
    }
}