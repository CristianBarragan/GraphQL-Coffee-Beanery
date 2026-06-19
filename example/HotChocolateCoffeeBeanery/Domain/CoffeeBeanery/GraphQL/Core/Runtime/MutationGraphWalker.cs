using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    /// <summary>
    /// Walks a GraphQL mutation input object once and produces, per entity alias, the ordered
    /// list of (column, value) pairs to upsert - directly from NodeRegistry's compiled index
    /// (ChildAliasByField to descend into nested input objects, ColumnByField/EnumByField to
    /// resolve scalar leaves). A single input field can resolve to multiple (entityAlias,
    /// entityColumn) destinations - needed for a model-only multi-entity aggregate like
    /// Product, where one source field can route to more than one backing entity/column.
    ///
    /// IMPORTANT: callers must pass the alias/node pair for the *unwrapped* entity payload,
    /// not the outer mutation-input wrapper. The wrapper typically holds the real payload
    /// under a single nested object/list field (e.g. `customerCustomerEdge`) alongside scalar
    /// metadata fields (e.g. `cacheKey`). Resolve the target alias from the GraphQL
    /// selection/field context (NOT from an input-level discriminator field), then pull out
    /// the wrapper's one nested object/list field and pass its value here. See
    /// ProcessService.MutationProcessAsync for the unwrap step.
    /// </summary>
    public static class MutationGraphWalker
    {
        public sealed class Result
        {
            // alias -> ordered (column, literal-SQL-ready value) pairs, insertion order
            // preserved so generated INSERT column lists are deterministic.
            public Dictionary<string, List<(string Column, string Value)>> ColumnsByAlias { get; } =
                new(StringComparer.OrdinalIgnoreCase);

            // aliases in the order first encountered - used downstream to seed BFS traversal
            // without re-deriving it from dictionary keys.
            public List<string> AliasOrder { get; } = new();

            public void Add(string alias, string column, string value)
            {
                if (!ColumnsByAlias.TryGetValue(alias, out var list))
                {
                    list = new List<(string Column, string Value)>();
                    ColumnsByAlias[alias] = list;
                    AliasOrder.Add(alias);
                }

                // last-write-wins for the same column within the same alias (mirrors the
                // previous TryAdd-into-dictionary dedup behavior on RelationshipKey).
                for (var i = 0; i < list.Count; i++)
                {
                    if (string.Equals(list[i].Column, column, StringComparison.OrdinalIgnoreCase))
                    {
                        list[i] = (column, value);
                        return;
                    }
                }

                list.Add((column, value));
            }
        }

        /// <summary>
        /// rootAlias is the *already-unwrapped* entity's alias (e.g. "CustomerCustomerEdge"),
        /// resolved by the caller from the GraphQL selection/field context.
        /// inputNode is that entity's own value node (an ObjectValueNode for a single
        /// upsert, or a ListValueNode for a batch - both handled below) - i.e. the value of
        /// the wrapper's nested object/list field, already unwrapped by the caller.
        /// </summary>
        public static Result Walk(string rootAlias, IValueNode inputNode)
        {
            var result = new Result();
            WalkNode(rootAlias, inputNode, result);
            return result;
        }

        private static void WalkNode(string alias, IValueNode node, Result result)
        {
            switch (node)
            {
                case ListValueNode list:
                    foreach (var item in list.Items)
                        WalkNode(alias, item, result);
                    return;

                case ObjectValueNode obj:
                    foreach (var field in obj.Fields)
                        WalkField(alias, field, result);
                    return;

                default:
                    return;
            }
        }

        private static void WalkField(string alias, ObjectFieldNode field, Result result)
        {
            var fieldName = field.Name.Value;

            switch (field.Value)
            {
                case ObjectValueNode nestedObj:
                    if (NodeRegistry.FrozenChildAliasByField.TryGetValue((alias, fieldName), out var childAlias))
                    {
                        WalkNode(childAlias, nestedObj, result);
                    }
                    else
                    {
                        Console.WriteLine($"[MISS-CHILD-OBJ] alias={alias} field={fieldName}");
                    }
                    return;

                case ListValueNode nestedList:
                    if (NodeRegistry.FrozenChildAliasByField.TryGetValue((alias, fieldName), out var childAliasForList))
                    {
                        WalkNode(childAliasForList, nestedList, result);
                    }
                    else
                    {
                        Console.WriteLine($"[MISS-CHILD-LIST] alias={alias} field={fieldName}");
                    }
                    return;

                case NullValueNode:
                    return; // explicit null input - nothing to set

                default:
                    // FIX: TryResolveLeafField now returns a List<(EntityAlias, EntityColumn)>
                    // instead of a single pair - one input field can fan out to multiple real
                    // entity columns (the Product/multi-entity-aggregate case). Write the
                    // resolved value to every destination this field maps to.
                    if (!NodeRegistry.TryResolveLeafField(alias, fieldName, out var resolvedFields, out var enumMap))
                    {
                        Console.WriteLine($"[MISS-LEAF] alias={alias} field={fieldName}");
                        return; // unmapped field (e.g. "model", "cacheKey" wrapper-only fields) - skip silently
                    }

                    var rawValue = field.Value.Value?.ToString();
                    if (rawValue is null)
                        return;

                    var resolvedValue = ResolveEnumOrRaw(rawValue, enumMap);

                    foreach (var (entityAlias, entityColumn) in resolvedFields)
                        result.Add(entityAlias, entityColumn, resolvedValue);

                    return;
            }
        }

        private static string ResolveEnumOrRaw(string rawValue, Dictionary<string, int>? enumMap)
        {
            if (enumMap is null || enumMap.Count == 0)
                return rawValue;

            return enumMap.TryGetValue(rawValue, out var intValue)
                ? intValue.ToString()
                : rawValue; // unmapped enum literal - pass through rather than silently drop
        }
    }
}