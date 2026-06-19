using System.Collections.Frozen;
using CoffeeBeanery.GraphQL.Core.GraphQL;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    /// <summary>
    /// Central registry populated by each mapping class's source-generated Register()
    /// override (see NodeTreeEmitter). Holds both the descriptive trees (ModelTrees /
    /// EntityTrees - used for graph-shape decisions: which alias is a child of which,
    /// what its upsert key column is named, etc.) and the compiled delegates that do the
    /// actual per-row work (construct a model, copy entity fields onto it, read its key,
    /// attach a child into a parent) without reflection.
    ///
    /// Registration happens once at startup as each mapping class's static initializer
    /// (or DI-triggered instantiation) calls Register(). After that, Freeze() snapshots
    /// every table into a FrozenDictionary so the row-processing hot path in
    /// DynamicGraphMaterializer / QueryHandler never touches a mutable Dictionary.
    /// </summary>
    public static class NodeRegistry
    {
        // ---------------------------------------------------------------- descriptive trees

        public static Dictionary<string, ModelNode> ModelNodes { get; } = new();
        public static Dictionary<string, EntityNode> EntityNodes { get; } = new();

        public static Dictionary<string, ModelNodeTree> ModelTrees { get; } = new();
        public static Dictionary<string, EntityNodeTree> EntityTrees { get; } = new();

        // ---------------------------------------------------------------- per-field node index
        // (unchanged from the existing reflection-era registry - still used for field-level
        // lookups outside the hot materialization loop, e.g. schema/introspection tooling)

        private static readonly Dictionary<string, object> _nodesByKey =
            new(StringComparer.Ordinal);

        public static void RegisterNode(
            string relationshipKey,
            string lookupKey,
            object node,
            Type modelType,
            Type entityOrModelType,
            bool isEntity)
        {
            _nodesByKey[lookupKey] = node;
        }

        public static bool TryGetNode(string lookupKey, out object? node) =>
            _nodesByKey.TryGetValue(lookupKey, out node);

        // ---------------------------------------------------------------- compiled delegates
        //
        // Populated directly by NodeTreeEmitter-generated Register() overrides as ordinary,
        // statically-typed C# lambdas - e.g. `() => new Account()`, or
        // `(entity, model) => { ((Account)entity).Name = ... }`. No PropertyInfo, no
        // Activator.CreateInstance(Type), so these are AOT/trim safe and fast: each call site
        // is a direct method/field reference the trimmer can see and keep.

        /// <summary>alias -> parameterless model constructor</summary>
        public static Dictionary<string, Func<object>> ModelFactories { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>alias -> (entityInstance, modelInstance) => copies this alias's FieldMaps</summary>
        public static Dictionary<string, Action<object, object>> EntityToModelAppliers { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>alias -> entityInstance => upsert-key string (pre-split, no per-row Split('~'))</summary>
        public static Dictionary<string, Func<object, string?>> KeyGetters { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// (parentAlias, childAlias) -> (parentModel, childModel) => attaches child into the
        /// correct navigation property on parent (single reference or list, decided at
        /// generation time from the resolved navigation shape - no per-row type-shape scan).
        /// </summary>
        public static Dictionary<(string ParentAlias, string ChildAlias), Action<object, object>> Attachers { get; } =
            new();

        // ---------------------------------------------------------------- frozen snapshot
        //
        // All tables above are write-once (during startup registration) / read-many (one
        // lookup per row, per alias, for the lifetime of the process). FrozenDictionary
        // trades slower construction for faster lookups, which is exactly this access
        // pattern. Call Freeze() once after every mapping class has registered (e.g. from a
        // hosted-service StartAsync, or lazily on first query if you don't have an explicit
        // startup hook).

        private static bool _frozen;
        private static readonly object _freezeLock = new();

        public static FrozenDictionary<string, ModelNodeTree> FrozenModelTrees { get; private set; } =
            FrozenDictionary<string, ModelNodeTree>.Empty;

        public static FrozenDictionary<string, EntityNodeTree> FrozenEntityTrees { get; private set; } =
            FrozenDictionary<string, EntityNodeTree>.Empty;

        public static FrozenDictionary<string, Func<object>> FrozenModelFactories { get; private set; } =
            FrozenDictionary<string, Func<object>>.Empty;

        public static FrozenDictionary<string, Action<object, object>> FrozenEntityToModelAppliers { get; private set; } =
            FrozenDictionary<string, Action<object, object>>.Empty;

        public static FrozenDictionary<string, Func<object, string?>> FrozenKeyGetters { get; private set; } =
            FrozenDictionary<string, Func<object, string?>>.Empty;

        public static FrozenDictionary<(string ParentAlias, string ChildAlias), Action<object, object>> FrozenAttachers { get; private set; } =
            FrozenDictionary<(string ParentAlias, string ChildAlias), Action<object, object>>.Empty;

        public static void Freeze()
        {
            if (_frozen) return;

            lock (_freezeLock)
            {
                if (_frozen) return;

                FrozenModelTrees = ModelTrees.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
                FrozenEntityTrees = EntityTrees.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
                FrozenModelFactories = ModelFactories.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
                FrozenEntityToModelAppliers = EntityToModelAppliers.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
                FrozenKeyGetters = KeyGetters.ToFrozenDictionary(StringComparer.OrdinalIgnoreCase);
                FrozenAttachers = Attachers.ToFrozenDictionary();
                FrozenChildAliasByField = ChildAliasByField.ToFrozenDictionary();
                FrozenColumnByField = ColumnByField.ToFrozenDictionary();
                FrozenEnumByField = EnumByField.ToFrozenDictionary();

                _frozen = true;
            }
        }

        /// <summary>
        /// Test/dev hook - allows re-registration (e.g. hot reload, test isolation) by
        /// dropping the frozen snapshot. Do not call this in steady-state production code.
        /// </summary>
        public static void Unfreeze() => _frozen = false;

        // ---------------------------------------------------------------- GraphQL path index
        //
        // Resolves a nested GraphQL selection path (e.g. customerCustomerEdge -> innerCustomer
        // -> customerKey) straight to (entityAlias, entityColumn) without walking
        // ModelChildren/FieldMaps lists at request time. Every entry is a compile-time literal
        // emitted by NodeTreeEmitter - the only runtime value is the mapping's own alias.

        /// <summary>(parentAlias, graphQlFieldName) -> childAlias, for object-typed (navigation) fields.</summary>
        public static Dictionary<(string ParentAlias, string FieldName), string> ChildAliasByField { get; } = new();

        /// <summary>(alias, graphQlFieldName) -> (entityAlias, entityColumn), for leaf scalar fields.</summary>
        public static Dictionary<(string Alias, string FieldName), List<(string EntityAlias, string EntityColumn)>> ColumnByField { get; } = new();

        public static FrozenDictionary<(string ParentAlias, string FieldName), string> FrozenChildAliasByField { get; private set; } =
            FrozenDictionary<(string ParentAlias, string FieldName), string>.Empty;

        public static FrozenDictionary<(string Alias, string FieldName), List<(string EntityAlias, string EntityColumn)>> FrozenColumnByField { get; private set; } =
            FrozenDictionary<(string Alias, string FieldName), List<(string EntityAlias, string EntityColumn)>>.Empty;

        /// <summary>(alias, graphQlFieldName) -> enum name->int dictionary, for enum-typed leaf fields.
        /// Only populated for fields whose FieldMap carried FromEnum/ToEnum data.</summary>
        public static Dictionary<(string Alias, string FieldName), Dictionary<string, int>> EnumByField { get; } = new();

        public static FrozenDictionary<(string Alias, string FieldName), Dictionary<string, int>> FrozenEnumByField { get; private set; } =
            FrozenDictionary<(string Alias, string FieldName), Dictionary<string, int>>.Empty;

        /// <summary>
        /// Walks pathSegments[0..^2] as navigation hops from rootAlias, then resolves the
        /// final segment as a scalar field. O(pathSegments.Count) FrozenDictionary lookups,
        /// no reflection, no list scans. Returns null if any hop or the leaf isn't registered
        /// (e.g. the GraphQL field has no corresponding mapping).
        /// </summary>
        public static List<(string EntityAlias, string EntityColumn)> ResolvePath(
            string rootAlias, IReadOnlyList<string> pathSegments)
        {
            if (pathSegments.Count == 0)
                return null;

            var alias = rootAlias;

            for (var i = 0; i < pathSegments.Count - 1; i++)
            {
                if (!FrozenChildAliasByField.TryGetValue((alias, pathSegments[i]), out var next))
                    return null;

                alias = next;
            }

            return FrozenColumnByField.TryGetValue((alias, pathSegments[^1]), out var result)
                ? result
                : default;
        }

        /// <summary>
        /// Single-hop leaf resolution used by the mutation walker: given the alias the walker
        /// is currently inside and a scalar field name on it, returns the destination
        /// (entityAlias, entityColumn) plus its enum dictionary if any - one pair of
        /// FrozenDictionary lookups, no path-walking needed since the walker already tracks
        /// "current alias" itself as it descends the GraphQL input tree.
        /// </summary>
        public static bool TryResolveLeafField(
            string alias, string fieldName,
            out List<(string EntityAlias, string EntityColumn)> fields,
            out Dictionary<string, int>? enumMap)
            // out string entityAlias, out string entityColumn,
            // out Dictionary<string, int>? enumMap)
        {
            enumMap = null;

            if (!FrozenColumnByField.TryGetValue((alias, fieldName), out fields))
                return false;
            
            FrozenEnumByField.TryGetValue((alias, fieldName), out enumMap);
            return true;
        }
    }
}