// HydrationRuntimeContext.cs — mirrors HydrationContext for QueryHandler
namespace CoffeeBeanery.GraphQL.Core.Runtime;

public sealed class HydrationRuntimeContext
{
    public GraphIL                   Graph         { get; init; } = null!;
    public Dictionary<string, Type>  SplitOnDapper { get; init; } = new();
    public PaginationContext?        PaginationContext    { get; init; }

    public string SelectSql { get; set; }
}