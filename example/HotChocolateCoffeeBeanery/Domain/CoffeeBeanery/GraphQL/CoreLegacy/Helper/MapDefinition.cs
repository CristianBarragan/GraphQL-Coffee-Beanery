namespace CoffeeBeanery.GraphQL.Core.Configuration;

public sealed class MappingDefinition
{
    public Type Source { get; init; } = default!;
    public Type Destination { get; init; } = default!;
    public object[] PropertyMappings { get; init; } = default!;
}