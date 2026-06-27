using System.Collections.Frozen;

namespace CoffeeBeanery.GraphQL.Core.Sql;

public static class NodeRegistry
{
    public static Graph Graph { get; private set; } = new();

    public static Dictionary<(string Alias, string Field), string> ChildAliasByField { get; } = new();
    public static Dictionary<(string Alias, string Field), List<(string EntityAlias, string EntityColumn)>> ColumnByField { get; } = new();
    public static Dictionary<(string Alias, string Field), Dictionary<string, int>> EnumByField { get; } = new();

    public static FrozenDictionary<(string Alias, string Field), string> FrozenChildAliasByField { get; private set; } =
        FrozenDictionary<(string Alias, string Field), string>.Empty;

    public static FrozenDictionary<(string Alias, string Field), List<(string EntityAlias, string EntityColumn)>> FrozenColumnByField { get; private set; } =
        FrozenDictionary<(string Alias, string Field), List<(string EntityAlias, string EntityColumn)>>.Empty;

    public static FrozenDictionary<(string Alias, string Field), Dictionary<string, int>> FrozenEnumByField { get; private set; } =
        FrozenDictionary<(string Alias, string Field), Dictionary<string, int>>.Empty;

    public static Dictionary<string, ModelNodeTree> ModelTrees { get; } = new();
    public static Dictionary<string, EntityNodeTree> EntityTrees { get; } = new();

    public static FrozenDictionary<string, ModelNodeTree> FrozenModelTrees { get; private set; } =
        FrozenDictionary<string, ModelNodeTree>.Empty;

    public static FrozenDictionary<string, EntityNodeTree> FrozenEntityTrees { get; private set; } =
        FrozenDictionary<string, EntityNodeTree>.Empty;

    public static Dictionary<(string ParentAlias, string ChildAlias), Action<object, object>> Attachers { get; } = new();

    public static FrozenDictionary<(string ParentAlias, string ChildAlias), Action<object, object>> FrozenAttachers { get; private set; } =
        FrozenDictionary<(string ParentAlias, string ChildAlias), Action<object, object>>.Empty;

    public static Dictionary<string, Func<object>> ModelFactories { get; } = new();
    public static Dictionary<string, Action<object, object>> EntityToModelAppliers { get; } = new();
    public static Dictionary<string, Func<object, string?>> KeyGetters { get; } = new();

    public static FrozenDictionary<string, Func<object>> FrozenModelFactories { get; private set; } =
        FrozenDictionary<string, Func<object>>.Empty;

    public static FrozenDictionary<string, Action<object, object>> FrozenEntityToModelAppliers { get; private set; } =
        FrozenDictionary<string, Action<object, object>>.Empty;

    public static FrozenDictionary<string, Func<object, string?>> FrozenKeyGetters { get; private set; } =
        FrozenDictionary<string, Func<object, string?>>.Empty;

    public static Dictionary<(string Alias, string FieldName), GraphEdge> EdgeByAliasAndField { get; private set; } = new();

    public static FrozenDictionary<(string Alias, string FieldName), GraphEdge> FrozenEdgeByAliasAndField { get; private set; } =
        FrozenDictionary<(string Alias, string FieldName), GraphEdge>.Empty;

    public static void Register(Graph graph)
    {
        Graph = graph;
        EdgeByAliasAndField = graph.Edges
            .Where(e => e.Kind == GraphEdgeKind.ModelToEntity)
            .GroupBy(e => (e.FromAlias, e.FieldName))
            .ToDictionary(g => g.Key, g => g.Last());
    }

    public static void Freeze()
    {
        FrozenModelTrees = ModelTrees.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        FrozenEntityTrees = EntityTrees.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        FrozenChildAliasByField = ChildAliasByField.ToFrozenDictionary();
        FrozenColumnByField = ColumnByField.ToFrozenDictionary();
        FrozenEnumByField = EnumByField.ToFrozenDictionary();
        FrozenAttachers = Attachers.ToFrozenDictionary();
        FrozenModelFactories = ModelFactories.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        FrozenEntityToModelAppliers = EntityToModelAppliers.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        FrozenKeyGetters = KeyGetters.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
        FrozenEdgeByAliasAndField = EdgeByAliasAndField.ToFrozenDictionary();
    }

    public static IReadOnlyList<(string EntityAlias, string EntityColumn)> ResolveLeaf(string alias, string field)
        => FrozenColumnByField.TryGetValue((alias, field), out var cols)
            ? cols
            : Array.Empty<(string EntityAlias, string EntityColumn)>();
}