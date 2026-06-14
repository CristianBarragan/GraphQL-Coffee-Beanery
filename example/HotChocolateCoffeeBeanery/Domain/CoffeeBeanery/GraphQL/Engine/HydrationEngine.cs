using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.Runtime;

namespace CoffeeBeanery.GraphQL.Engine;

// Describes how a single column in the flat Dapper result row maps
// to a member on the target model type T.
public sealed class FieldMapping
{
    public MemberInfo Member     { get; init; } = null!;
    public Type       MemberType { get; init; } = null!;
    public int        Ordinal    { get; init; }
}

// Carries everything QueryEngine needs to project object[] rows → T.
public sealed class HydrationContext
{
    public GraphIL                    Graph         { get; init; } = null!;
    public Dictionary<string, Type>   SplitOnDapper { get; init; } = new();
    public IReadOnlyList<FieldMapping> Mappings      { get; init; } = Array.Empty<FieldMapping>();
    public PaginationContext?         PaginationContext    { get; init; }

    // Stable cache key derived from the alias set and ordinal layout.
    public string CacheKey => string.Join("|", Mappings.Select(m => $"{m.Ordinal}:{m.Member.Name}"));
}

public interface IHydrationEngine
{
    HydrationContext BuildContext(
        GraphIL graph,
        Dictionary<string, Type> splitOnDapper);

    Func<object[], T> GetOrCreate<T>(HydrationContext context) where T : class, new();
}

public sealed class HydrationEngine : IHydrationEngine
{
    private readonly ConcurrentDictionary<string, Delegate> _projectorCache = new();

    // Builds a HydrationContext by correlating the ordered splitOnDapper columns
    // with GraphILField definitions on each node.
    public HydrationContext BuildContext(
        GraphIL graph,
        Dictionary<string, Type> splitOnDapper)
    {
        var mappings = new List<FieldMapping>();
        var ordinal  = 0;

        // splitOnDapper is ordered — each entry is one Dapper split segment.
        // Key = "alias~SplitColumn" or just "SplitColumn"; Value = CLR type for that segment.
        foreach (var (splitKey, clrType) in splitOnDapper.Where(a => a.Value != null))
        {
            var alias = splitKey.Contains('~')
                ? splitKey.Split('~')[0]
                : clrType.Name;

            if (!graph.Nodes.TryGetValue(alias, out var node))
            {
                ordinal++;
                continue;
            }

            // Match GraphILFields → properties on clrType
            foreach (var field in node.Fields)
            {
                var prop = clrType.GetProperty(
                    field.DestinationName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

                if (prop == null) continue;

                mappings.Add(new FieldMapping
                {
                    Member     = prop,
                    MemberType = prop.PropertyType,
                    Ordinal    = ordinal
                });
            }

            ordinal++;
        }

        return new HydrationContext
        {
            Graph         = graph,
            SplitOnDapper = splitOnDapper,
            Mappings      = mappings
        };
    }

    // Returns a compiled expression-tree projector that maps object[] → T.
    // Projectors are cached by the context's CacheKey.
    public Func<object[], T> GetOrCreate<T>(HydrationContext context) where T : class, new()
    {
        var key = $"{typeof(T).FullName}:{context.CacheKey}";

        return (Func<object[], T>)_projectorCache.GetOrAdd(key, _ => BuildProjector<T>(context));
    }

    private static Func<object[], T> BuildProjector<T>(HydrationContext context)
    {
        var rowParam  = Expression.Parameter(typeof(object[]), "row");
        var bindings  = new List<MemberBinding>();

        foreach (var mapping in context.Mappings)
        {
            var indexExpr = Expression.ArrayIndex(rowParam, Expression.Constant(mapping.Ordinal));

            var safeValue = Expression.Condition(
                Expression.Equal(indexExpr, Expression.Constant(null)),
                Expression.Default(mapping.MemberType),
                Expression.Convert(indexExpr, mapping.MemberType));

            bindings.Add(Expression.Bind(mapping.Member, safeValue));
        }

        var body = Expression.MemberInit(Expression.New(typeof(T)), bindings);

        return Expression.Lambda<Func<object[], T>>(body, rowParam).Compile();
    }
}