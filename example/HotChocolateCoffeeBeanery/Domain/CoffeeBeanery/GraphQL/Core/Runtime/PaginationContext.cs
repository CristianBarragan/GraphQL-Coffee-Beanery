namespace CoffeeBeanery.GraphQL.Core.Runtime;

public sealed class PaginationContext
{
    public int?   StartCursor { get; init; }
    public int?   EndCursor   { get; init; }
    public int    First       { get; set; }
    public int    Last        { get; set; }
    public string? Before     { get; set; }
    public string? After      { get; set; }
    public bool   RequiresTotalCount { get; set; }
}