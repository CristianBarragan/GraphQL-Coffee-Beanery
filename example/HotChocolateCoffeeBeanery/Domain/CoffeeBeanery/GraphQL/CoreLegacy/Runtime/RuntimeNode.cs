using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public sealed class RuntimeNode
{
    public SqlNode SqlNode { get; init; } = null!;

    public RuntimeNode? Parent { get; set; }

    public List<RuntimeNode> Children { get; } = new();

    // Precompiled actions
    public Action<object, object>? MapModelToEntity { get; set; }
    public Action<object, object>? MapEntityToModel { get; set; }
}