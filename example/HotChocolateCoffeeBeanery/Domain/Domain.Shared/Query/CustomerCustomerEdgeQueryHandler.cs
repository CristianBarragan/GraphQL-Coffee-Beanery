using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.Service;
using Domain.Model;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CoffeeBeanery.GraphQL.Helper;

namespace Domain.Shared.Query
{
    public class CustomerCustomerEdgeQueryHandler<M> : ProcessQuery<M>,
        IQuery<ProcessQueryParameters,
            (List<M> list, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>
        where M : class
    {
        private readonly IMapper _mapper;

        public CustomerCustomerEdgeQueryHandler(
            ILoggerFactory loggerFactory,
            NpgsqlConnection dbConnection,
            IMapper mapper)
            : base(loggerFactory, dbConnection)
        {
            _mapper = mapper;
        }

        public override (List<M> models, int? startCursor, int? endCursor, int? totalCount,
            int? totalPageRecords)
        MappingConfiguration(
            List<M> models,
            SqlStructure sqlStructure,
            object[] map,
            List<Type> allTypes,
            List<Type> types,
            Dictionary<string, SqlNode> sqlNodesApplied,
            NodeTree relativeNodeTree,
            Dictionary<string, NodeTree> modelTrees,
            Dictionary<string, NodeTree> entityTrees)
        {
            int totalCount  = 0;
            int pageRecords = 0;

            var wrappers   = models.OfType<Wrapper>().ToList();
            var aliasIndex = BuildAliasIndex(sqlStructure.EntityMapping);
            var seenCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // ── Phase 1: map each raw Dapper object → model via IMapper ──────────
            var objectMap = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

            for (int i = 0; i < map.Length; i++)
            {
                if (map[i] == null) continue;
                if (map[i] is TotalPageRecords tp) { pageRecords = tp.PageRecords; continue; }
                if (map[i] is TotalRecordCount  tr) { totalCount  = tr.RecordCount; continue; }

                var entityType = map[i].GetType();
                var entityName = entityType.Name;
                var alias      = aliasIndex.TryGetValue(i, out var a) ? a : entityName;
                var seen       = seenCounts.GetValueOrDefault(entityName, 0);
                seenCounts[entityName] = seen + 1;

                var entityTree = ResolveEntityTree(
                    alias, entityType, types.ElementAtOrDefault(i), seen, entityTrees);

                if (entityTree == null) continue;

                try
                {
                    objectMap[entityTree.Alias] = _mapper.MapByAlias(
                        entityType, map[i], entityTree.Alias);
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(
                        $"[WARN] MapByAlias failed for '{entityTree.Alias}': {ex.Message}");
                    Console.ResetColor();
                }
            }

            // ── Phase 2: find root model tree ─────────────────────────────────────
            var rootModelTree = GetRootFromWrapper<M>(modelTrees);

            // ── Phase 3: build the full object graph recursively ──────────────────
            // NodeResult carries:
            //   Objects  — alias → already-instantiated model instance (shared state)
            //   so when the same alias is visited again (e.g. Customer appearing as
            //   both InnerCustomer and OuterCustomer), we reuse the existing instance
            var nodeResult = new NodeResult
            {
                RootObject   = new Wrapper(),
                CurrentObject = new Wrapper(),
                ModelTypes   = allTypes,
                Objects      = new Dictionary<string, object>(
                    StringComparer.OrdinalIgnoreCase)
            };

            // Seed Objects with everything already mapped in Phase 1
            foreach (var kv in objectMap)
                nodeResult.Objects[kv.Key] = kv.Value;

            var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            Build(rootModelTree, objectMap, modelTrees, entityTrees, nodeResult, visited);

            // ── Phase 4: attach root list to Wrapper ──────────────────────────────
            var wrapperObj = (Wrapper)nodeResult.RootObject;

            // Find the root model instance (e.g. CustomerCustomerEdge) and attach
            if (nodeResult.Objects.TryGetValue(rootModelTree.Alias, out var rootObj))
                Attach(wrapperObj, rootObj, nodeResult);

            wrappers.Add(wrapperObj);

            return (wrappers.OfType<M>().ToList(),
                sqlStructure.Pagination?.StartCursor,
                sqlStructure.Pagination?.EndCursor,
                totalCount, pageRecords);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // BUILD
        // ─────────────────────────────────────────────────────────────────────────

        private void Build(
            NodeTree modelTree,
            Dictionary<string, object> objectMap,
            Dictionary<string, NodeTree> modelTrees,
            Dictionary<string, NodeTree> entityTrees,
            NodeResult nodeResult,
            HashSet<string> visited)
        {
            if (!nodeResult.Objects.TryGetValue(modelTree.Alias, out var currentModel))
            {
                currentModel = objectMap.TryGetValue(modelTree.Alias, out var mapped)
                    ? mapped
                    : Activator.CreateInstance(modelTree.ModelType)!;

                nodeResult.Objects[modelTree.Alias] = currentModel;
            }

            nodeResult.CurrentObject = currentModel;

            // ------------------------------------------------------------------
            // MODEL CHILDREN
            // ------------------------------------------------------------------
            foreach (var linkKey in modelTree.ModelChildren ?? Enumerable.Empty<LinkKey>())
            {
                var childAlias = !string.IsNullOrWhiteSpace(linkKey.AliasTo)
                    ? linkKey.AliasTo
                    : linkKey.To;

                NodeTree? childModelTree = null;

                if (modelTrees.TryGetValue(childAlias, out var directModel))
                {
                    childModelTree = directModel;
                }
                else
                {
                    var childEntityTree = ResolveChildEntityTree(childAlias, entityTrees);

                    if (childEntityTree != null)
                    {
                        childModelTree = ResolveModelTreeForEntity(
                            childEntityTree,
                            childAlias,
                            modelTrees);
                    }
                }

                if (childModelTree == null)
                    continue;

                Build(
                    childModelTree,
                    objectMap,
                    modelTrees,
                    entityTrees,
                    nodeResult,
                    visited);

                if (nodeResult.Objects.TryGetValue(childModelTree.Alias, out var childObj))
                {
                    Attach(
                        currentModel,
                        childObj,
                        nodeResult,
                        childModelTree.Prefix,
                        childModelTree.Alias);
                }
            }

            // ------------------------------------------------------------------
            // ENTITY CHILDREN
            // ------------------------------------------------------------------
            if (!modelTree.IsEntity)
                return;

            var entityTree = entityTrees.TryGetValue(modelTree.Alias, out var et)
                ? et
                : entityTrees.Values.FirstOrDefault(t =>
                    t.ModelType == modelTree.ModelType &&
                    t.IsEntity);

            if (entityTree == null)
                return;

            foreach (var childLink in entityTree.Children.Concat(entityTree.RelatedChildren))
            {
                var childAlias = !string.IsNullOrWhiteSpace(childLink.AliasTo)
                    ? childLink.AliasTo
                    : childLink.To;

                var childEntityTree = ResolveChildEntityTree(
                    childAlias,
                    entityTrees);

                if (childEntityTree == null)
                    continue;

                var childModelTree = ResolveModelTreeForEntity(
                    childEntityTree,
                    childAlias,
                    modelTrees);

                if (childModelTree == null)
                    continue;

                Build(
                    childModelTree,
                    objectMap,
                    modelTrees,
                    entityTrees,
                    nodeResult,
                    visited);

                if (nodeResult.Objects.TryGetValue(childModelTree.Alias, out var childObj))
                {
                    Attach(
                        currentModel,
                        childObj,
                        nodeResult,
                        childModelTree.Prefix,
                        childModelTree.Alias);
                }
            }

            ProjectAggregates(nodeResult, modelTrees);
        }
        
        private static void ProjectAggregates(
            NodeResult nodeResult,
            Dictionary<string, NodeTree> modelTrees)
        {
            foreach (var modelTree in modelTrees.Values.Where(t => t.IsModel))
            {
                if (!nodeResult.Objects.TryGetValue(modelTree.Alias, out var targetModel))
                    continue;

                var targetType = targetModel.GetType();

                foreach (var field in modelTree.Mapping)
                {
                    var sourceAlias = field.DestinationAlias;

                    if (string.IsNullOrWhiteSpace(sourceAlias))
                        continue;

                    if (!nodeResult.Objects.TryGetValue(sourceAlias, out var sourceObj))
                        continue;

                    var sourceProp = sourceObj.GetType()
                        .GetProperty(field.DestinationName.Split('.').Last());

                    var targetProp = targetType
                        .GetProperty(field.SourceName);

                    if (sourceProp == null || targetProp == null)
                        continue;

                    var value = sourceProp.GetValue(sourceObj);

                    SafeSet(targetProp, targetModel, value);
                }
            }
        }
        
        private static void SafeSet(PropertyInfo prop, object instance, object value)
        {
            if (prop == null || value == null)
                return;

            var targetType = prop.PropertyType;

            if (targetType.IsGenericType &&
                typeof(System.Collections.IEnumerable).IsAssignableFrom(targetType))
            {
                var elementType = targetType.GetGenericArguments()[0];

                var existing = prop.GetValue(instance);

                if (existing == null)
                {
                    existing = Activator.CreateInstance(targetType);
                    prop.SetValue(instance, existing);
                }

                var list = (System.Collections.IList)existing;

                if (value is System.Collections.IEnumerable enumerable &&
                    value is not string)
                {
                    foreach (var v in enumerable)
                    {
                        if (v != null && elementType.IsAssignableFrom(v.GetType()))
                        {
                            if (!list.Contains(v))
                            {
                                list.Add(v);    
                            }
                        }
                    }
                }
                else
                {
                    if (elementType.IsAssignableFrom(value.GetType()))
                        list.Add(value);
                }

                return;
            }

            if (targetType.IsAssignableFrom(value.GetType()))
            {
                prop.SetValue(instance, value);
                return;
            }
            
            if (targetType == typeof(Guid) || targetType == typeof(Guid?))
            {
                var keyProp = value.GetType().GetProperty("CustomerKey");
                var key = keyProp?.GetValue(value);
                prop.SetValue(instance, key);
                return;
            }
        }
        
        private static void HydrateModel(
            NodeTree modelTree,
            object currentModel,
            NodeResult nodeResult)
        {
            var modelProperties = currentModel.GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanWrite);

            foreach (var property in modelProperties)
            {
                var currentValue = property.GetValue(currentModel);

                // Don't overwrite values already mapped
                if (currentValue != null &&
                    !Equals(currentValue, GetDefault(property.PropertyType)))
                    continue;

                foreach (var obj in nodeResult.Objects.Values)
                {
                    if (ReferenceEquals(obj, currentModel))
                        continue;

                    var sourceProperty = obj.GetType().GetProperty(
                        property.Name,
                        BindingFlags.Public |
                        BindingFlags.Instance |
                        BindingFlags.IgnoreCase);

                    if (sourceProperty == null)
                        continue;

                    if (!property.PropertyType.IsAssignableFrom(sourceProperty.PropertyType))
                        continue;

                    var sourceValue = sourceProperty.GetValue(obj);

                    if (sourceValue == null)
                        continue;

                    property.SetValue(currentModel, sourceValue);
                    break;
                }
            }
        }

        private static object? GetDefault(Type type)
        {
            return type.IsValueType
                ? Activator.CreateInstance(type)
                : null;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ATTACH
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Attaches <paramref name="child"/> onto <paramref name="parent"/> by finding
        /// the matching navigation property (scalar or List&lt;T&gt;) on the parent type.
        ///
        /// For List&lt;T&gt;: upserts by Guid key property ending with "Key".
        /// For scalar: only sets if the slot is currently null — preserves
        ///             InnerCustomer when OuterCustomer arrives.
        ///
        /// NodeResult.Objects is updated so subsequent visits reuse the same instance.
        /// </summary>
        private static void Attach(object parent, object child, NodeResult nodeResult, string prefix = "", string childAlias = "")
        {
            if (parent == null || child == null) return;

            var childType = child.GetType();
            var prop = ResolveNavigationProperty(parent.GetType(), childType, prefix, childAlias);
            
            if (prop == null) return;

            if (typeof(IList).IsAssignableFrom(prop.PropertyType))
            {
                var list = prop.GetValue(parent) as IList;
                if (list == null)
                {
                    list = (IList)Activator.CreateInstance(prop.PropertyType)!;
                    prop.SetValue(parent, list);
                }

                // Upsert by Guid key ending with "Key"
                var keyProp = childType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p =>
                        (p.PropertyType == typeof(Guid) ||
                         p.PropertyType == typeof(Guid?)) &&
                        p.Name.EndsWith("Key", StringComparison.OrdinalIgnoreCase));

                if (keyProp != null)
                {
                    var newKey = keyProp.GetValue(child);
                    if (newKey != null)
                    {
                        for (int j = 0; j < list.Count; j++)
                        {
                            if (Equals(keyProp.GetValue(list[j]!), newKey))
                            {
                                list[j] = child;
                                return;
                            }
                        }
                    }
                }

                if (!list.Contains(child))
                    list.Add(child);
            }
            else
            {
                // Scalar: prefer empty slot so InnerCustomer isn't overwritten
                // by OuterCustomer when both are of type Customer
                if (prop.GetValue(parent) == null)
                    prop.SetValue(parent, child);
            }

            // Keep nodeResult.Objects in sync with the parent's actual state
            // so future traversals find the same instance
            nodeResult.Objects[childAlias] = child;
        }

        // ─────────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────────

        private static NodeTree? ResolveChildEntityTree(
            string childAlias,
            Dictionary<string, NodeTree> entityTrees)
        {
            if (entityTrees.TryGetValue(childAlias, out var direct))
                return direct;

            var fallback = entityTrees.Values.FirstOrDefault(t =>
                t.Alias.Equals(childAlias, StringComparison.OrdinalIgnoreCase));

            if (fallback == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"[WARN] Child entity tree '{childAlias}' not found. Skipping.");
                Console.ResetColor();
            }

            return fallback;
        }

        private static NodeTree? ResolveModelTreeForEntity(
            NodeTree entityTree,
            string childAlias,
            Dictionary<string, NodeTree> modelTrees)
        {
            var result = modelTrees.Values
                .FirstOrDefault(t => t.ModelType == entityTree.ModelType)
                ?? modelTrees.Values.FirstOrDefault(t =>
                    t.Alias.Equals(childAlias, StringComparison.OrdinalIgnoreCase));

            if (result == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(
                    $"[WARN] No model tree for entity '{entityTree.Alias}'. Skipping.");
                Console.ResetColor();
            }

            return result;
        }

        private static NodeTree GetRootFromWrapper<TWrapper>(
            Dictionary<string, NodeTree> modelTrees)
        {
            var wrapperType = typeof(TWrapper);

            var rootListProp = wrapperType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p =>
                    p.PropertyType.IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(List<>));

            if (rootListProp == null)
                throw new InvalidOperationException(
                    $"{wrapperType.Name} has no List<T> root property");

            var rootModelType = rootListProp.PropertyType.GetGenericArguments()[0];

            return modelTrees.Values.FirstOrDefault(t => t.ModelType == rootModelType)
                ?? throw new InvalidOperationException(
                    $"No NodeTree found for root model type '{rootModelType.Name}'");
        }

        private static PropertyInfo? ResolveNavigationProperty(
            Type parentType,
            Type childType,
            string prefix,
            string childAlias)
        {
            if (!string.IsNullOrWhiteSpace(childAlias))
            {
                var aliasProp = parentType.GetProperty(
                    childAlias,
                    BindingFlags.Public |
                    BindingFlags.Instance |
                    BindingFlags.IgnoreCase);

                if (aliasProp != null)
                    return aliasProp;
            }

            return parentType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(a => a.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && a.Name.Matches(prefix))
                .FirstOrDefault(p =>
                {
                    if (p.PropertyType == childType)
                        return true;

                    if (!p.PropertyType.IsGenericType)
                        return false;

                    return p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)
                           && p.PropertyType.GetGenericArguments()[0] == childType;
                });
        }

        private static NodeTree? ResolveEntityTree(
            string? alias, Type entityType, Type? modelType, int seen,
            Dictionary<string, NodeTree> entityTrees)
        {
            if (alias != null && entityTrees.TryGetValue(alias, out var byAlias))
                return byAlias;

            var candidates = entityTrees.Values
                .Where(t => t.EntityType == entityType &&
                            (modelType == null || t.ModelType == modelType))
                .OrderBy(t => t.Id).ToList();

            if (!candidates.Any())
                candidates = entityTrees.Values
                    .Where(t => t.EntityType?.Name.Equals(entityType.Name,
                        StringComparison.OrdinalIgnoreCase) == true)
                    .OrderBy(t => t.Id).ToList();

            return seen < candidates.Count
                ? candidates[seen]
                : candidates.FirstOrDefault();
        }

        private static Dictionary<int, string> BuildAliasIndex(
            Dictionary<string, Type> entityMapping)
        {
            var dict = new Dictionary<int, string>();
            int i = 0;
            foreach (var kv in entityMapping)
                dict[i++] = kv.Key;
            return dict;
        }
    }

    public class NodeResult
    {
        public object RootObject    { get; set; }
        public object CurrentObject { get; set; }

        /// <summary>
        /// Shared object cache: alias → model instance.
        /// Seeded from Phase 1 objectMap and updated as Build/Attach run,
        /// so revisiting a node always returns the same instance rather than
        /// creating a duplicate.
        /// </summary>
        public Dictionary<string, object> Objects { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public List<Type>     ModelTypes     { get; set; }
        public List<NodeTree> ModelNodeTrees { get; set; }
    }
}