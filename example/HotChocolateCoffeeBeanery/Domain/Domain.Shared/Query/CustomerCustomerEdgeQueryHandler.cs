using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.Service;
using Domain.Model;
using Npgsql;
using System.Collections;
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
        
        public override (List<M> models,
                         int? startCursor,
                         int? endCursor,
                         int? totalCount,
                         int? totalPageRecords)
        MappingConfiguration(
            List<object[]>               rowMatrix,
            SqlStructure                 sqlStructure,
            List<Type>                   allTypes,
            List<Type>                   types,
            Dictionary<string, SqlNode>  sqlNodesApplied,
            NodeTree                     relativeNodeTree,
            Dictionary<string, NodeTree> modelTrees,
            Dictionary<string, NodeTree> entityTrees)
        {
            int totalCount  = 0;
            int pageRecords = 0;

            var aliasIndex = BuildAliasIndex(sqlStructure.EntityMapping);

            var rootModelTree  = GetRootFromWrapper<M>(modelTrees);
            var rootAliasIndex = 0; // column index of the root entity in each row
            string rootAlias   = rootModelTree.Alias;

            foreach (var kv in aliasIndex)
            {
                if (kv.Value.Equals(rootAlias, StringComparison.OrdinalIgnoreCase))
                {
                    rootAliasIndex = kv.Key;
                    break;
                }
            }

            var rowsByRootKey = new Dictionary<Guid, List<object[]>>();
            var rootKeyOrder  = new List<Guid>(); // preserve result ordering
            var seenCounts    = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var row in rowMatrix)
            {
                if (row[rootAliasIndex] is TotalPageRecords tp) { pageRecords = tp.PageRecords; continue; }
                if (row[rootAliasIndex] is TotalRecordCount tr) { totalCount  = tr.RecordCount; continue; }

                var rootEntity  = row[rootAliasIndex];
                var rootKey    = Guid.Empty;

                if (rootEntity != null)
                {
                    var keyProp = rootEntity.GetType()
                        .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
                        .FirstOrDefault(p =>
                            (p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?)) &&
                            p.Name.EndsWith("Key", StringComparison.OrdinalIgnoreCase));

                    if (keyProp?.GetValue(rootEntity) is Guid g)
                        rootKey = g;
                }

                if (rootKey == Guid.Empty) continue; // skip rows with no identifiable root

                if (!rowsByRootKey.ContainsKey(rootKey))
                {
                    rowsByRootKey[rootKey] = new List<object[]>();
                    rootKeyOrder.Add(rootKey);
                }

                rowsByRootKey[rootKey].Add(row);
            }

            var wrappers = new List<Wrapper>();

            foreach (var rootKey in rootKeyOrder)
            {
                var groupRows = rowsByRootKey[rootKey];

                var rawByAlias = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase);
                seenCounts.Clear();

                foreach (var row in groupRows)
                {
                    seenCounts.Clear();

                    for (int i = 0; i < row.Length; i++)
                    {
                        if (row[i] == null) continue;
                        if (row[i] is TotalPageRecords || row[i] is TotalRecordCount) continue;

                        var entityType = row[i].GetType();
                        var entityName = entityType.Name;
                        var alias      = aliasIndex.TryGetValue(i, out var a) ? a : entityName;
                        var seen       = seenCounts.GetValueOrDefault(entityName, 0);
                        seenCounts[entityName] = seen + 1;

                        var entityTree = ResolveEntityTree(
                            alias, entityType, types.ElementAtOrDefault(i), seen, entityTrees);

                        if (entityTree == null) continue;

                        if (!rawByAlias.TryGetValue(entityTree.Alias, out var bucket))
                        {
                            bucket = new List<object>();
                            rawByAlias[entityTree.Alias] = bucket;
                        }

                        bucket.Add(row[i]);
                    }
                }

                var dedupedRaw = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase);
                foreach (var (alias, bucket) in rawByAlias)
                    dedupedRaw[alias] = DeduplicateByKey(bucket);

                var nodeResult = new NodeResult
                {
                    RootObject    = new Wrapper(),
                    CurrentObject = new Wrapper(),
                    ModelTypes    = allTypes,
                    RawObjects    = dedupedRaw,
                    Objects       = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase),
                    AllObjects    = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase),
                };

                Build(rootModelTree, modelTrees, entityTrees, nodeResult);

                var wrapperObj = (Wrapper)nodeResult.RootObject;

                if (nodeResult.Objects.TryGetValue(rootModelTree.Alias, out var rootObj))
                    Attach(wrapperObj, rootObj, nodeResult);

                wrappers.Add(wrapperObj);
            }

            return (
                wrappers.OfType<M>().ToList(),
                sqlStructure.Pagination?.StartCursor,
                sqlStructure.Pagination?.EndCursor,
                totalCount,
                pageRecords);
        }

        private List<object> MapAlias(
            string alias,
            NodeResult nodeResult)
        {
            if (!nodeResult.RawObjects.TryGetValue(alias, out var rawList) || rawList.Count == 0)
                return new List<object>();

            var mapped = new List<object>(rawList.Count);

            foreach (var raw in rawList)
            {
                try
                {
                    mapped.Add(_mapper.MapByAlias(raw.GetType(), raw, alias));
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine($"[WARN] MapByAlias failed for '{alias}': {ex.Message}");
                    Console.ResetColor();
                }
            }

            return mapped;
        }

        private void Build(
            NodeTree modelTree,
            Dictionary<string, NodeTree> modelTrees,
            Dictionary<string, NodeTree> entityTrees,
            NodeResult nodeResult)
        {
            if (!nodeResult.Objects.TryGetValue(modelTree.Alias, out var currentModel))
            {
                var mappedList = MapAlias(modelTree.Alias, nodeResult);

                if (mappedList.Count > 0)
                {
                    currentModel = mappedList[0];
                    nodeResult.Objects[modelTree.Alias]    = currentModel;
                    nodeResult.AllObjects[modelTree.Alias] = mappedList;
                }
                else
                {
                    currentModel = Activator.CreateInstance(modelTree.ModelType)!;
                    nodeResult.Objects[modelTree.Alias] = currentModel;
                }
            }

            nodeResult.CurrentObject = currentModel!;

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
                        childModelTree = ResolveModelTreeForEntity(childEntityTree, childAlias, modelTrees);
                }

                if (childModelTree == null) continue;

                Build(childModelTree, modelTrees, entityTrees, nodeResult);

                if (nodeResult.Objects.TryGetValue(childModelTree.Alias, out var childObj))
                    Attach(currentModel, childObj, nodeResult, childModelTree.Prefix, childModelTree.Alias);
            }

            if (!modelTree.IsEntity) return;

            var entityTree = entityTrees.TryGetValue(modelTree.Alias, out var et)
                ? et
                : entityTrees.Values.FirstOrDefault(t =>
                    t.ModelType == modelTree.ModelType && t.IsEntity);

            if (entityTree == null) return;

            foreach (var childLink in entityTree.Children.Concat(entityTree.RelatedChildren))
            {
                var childAlias   = !string.IsNullOrWhiteSpace(childLink.AliasTo) ? childLink.AliasTo : childLink.To;
                var childEntTree = ResolveChildEntityTree(childAlias, entityTrees);
                if (childEntTree == null) continue;

                var childModelTree = ResolveModelTreeForEntity(childEntTree, childAlias, modelTrees);
                if (childModelTree == null) continue;

                Build(childModelTree, modelTrees, entityTrees, nodeResult);

                if (nodeResult.Objects.TryGetValue(childModelTree.Alias, out var childObj))
                    Attach(currentModel, childObj, nodeResult, childModelTree.Prefix, childModelTree.Alias);
            }

            ProjectAggregates(nodeResult, modelTrees);
        }

        private static List<object> DeduplicateByKey(List<object> items)
        {
            if (items.Count == 0) return items;

            var keyProp = items[0].GetType()
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p =>
                    (p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?)) &&
                    p.Name.EndsWith("Key", StringComparison.OrdinalIgnoreCase));

            if (keyProp == null) return items;

            var seen   = new HashSet<Guid>();
            var result = new List<object>(items.Count);

            foreach (var item in items)
            {
                if (keyProp.GetValue(item) is Guid g && g != Guid.Empty && seen.Add(g))
                    result.Add(item);
            }

            return result;
        }


        private static void ProjectAggregates(
            NodeResult nodeResult,
            Dictionary<string, NodeTree> modelTrees)
        {
            foreach (var modelTree in modelTrees.Values.Where(t => t.IsModel))
            {
                if (!nodeResult.Objects.TryGetValue(modelTree.Alias, out var targetModel)) continue;

                var targetType = targetModel.GetType();

                foreach (var field in modelTree.Mapping)
                {
                    var sourceAlias = field.DestinationAlias;
                    if (string.IsNullOrWhiteSpace(sourceAlias)) continue;
                    if (!nodeResult.Objects.TryGetValue(sourceAlias, out var sourceObj)) continue;

                    var sourceProp = sourceObj.GetType().GetProperty(field.DestinationName.Split('.').Last());
                    var targetProp = targetType.GetProperty(field.SourceName);

                    if (sourceProp == null || targetProp == null) continue;

                    SafeSet(targetProp, targetModel, sourceProp.GetValue(sourceObj));
                }
            }
        }

        private static void Attach(
            object parent, object child, NodeResult nodeResult,
            string prefix = "", string childAlias = "")
        {
            if (parent == null || child == null) return;

            var childType = child.GetType();
            var prop      = ResolveNavigationProperty(parent.GetType(), childType, prefix, childAlias);
            if (prop == null) return;

            if (typeof(IList).IsAssignableFrom(prop.PropertyType))
            {
                var list = prop.GetValue(parent) as IList;
                if (list == null)
                {
                    list = (IList)Activator.CreateInstance(prop.PropertyType)!;
                    prop.SetValue(parent, list);
                }

                IEnumerable<object> candidates =
                    !string.IsNullOrWhiteSpace(childAlias) &&
                    nodeResult.AllObjects.TryGetValue(childAlias, out var bucket)
                        ? bucket
                        : new[] { child };

                var keyProp = childType
                    .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .FirstOrDefault(p =>
                        (p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?)) &&
                        p.Name.EndsWith("Key", StringComparison.OrdinalIgnoreCase));

                foreach (var item in candidates)
                {
                    if (item == null) continue;

                    if (keyProp != null)
                    {
                        var newKey = keyProp.GetValue(item);
                        if (newKey != null)
                        {
                            bool found = false;
                            for (int j = 0; j < list.Count; j++)
                            {
                                if (Equals(keyProp.GetValue(list[j]!), newKey))
                                {
                                    list[j] = item; found = true; break;
                                }
                            }
                            if (found) continue;
                        }
                    }

                    if (!list.Contains(item)) list.Add(item);
                }
            }
            else
            {
                if (prop.GetValue(parent) == null)
                    prop.SetValue(parent, child);
            }

            if (!string.IsNullOrWhiteSpace(childAlias))
                nodeResult.Objects[childAlias] = child;
        }

        private static void SafeSet(PropertyInfo prop, object instance, object value)
        {
            if (prop == null || value == null) return;
            var targetType = prop.PropertyType;

            if (targetType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(targetType))
            {
                var elementType = targetType.GetGenericArguments()[0];
                var existing    = prop.GetValue(instance);
                if (existing == null) { existing = Activator.CreateInstance(targetType); prop.SetValue(instance, existing); }
                var list = (IList)existing;

                if (value is IEnumerable enumerable && value is not string)
                {
                    foreach (var v in enumerable)
                        if (v != null && elementType.IsAssignableFrom(v.GetType()) && !list.Contains(v))
                            list.Add(v);
                }
                else if (elementType.IsAssignableFrom(value.GetType()) && !list.Contains(value))
                {
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
                prop.SetValue(instance, value.GetType().GetProperty("CustomerKey")?.GetValue(value));
            }
        }

        private static NodeTree? ResolveChildEntityTree(
            string childAlias, Dictionary<string, NodeTree> entityTrees)
        {
            if (entityTrees.TryGetValue(childAlias, out var direct)) return direct;
            var fallback = entityTrees.Values.FirstOrDefault(t =>
                t.Alias.Equals(childAlias, StringComparison.OrdinalIgnoreCase));
            if (fallback == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARN] Child entity tree '{childAlias}' not found. Skipping.");
                Console.ResetColor();
            }
            return fallback;
        }

        private static NodeTree? ResolveModelTreeForEntity(
            NodeTree entityTree, string childAlias,
            Dictionary<string, NodeTree> modelTrees)
        {
            var result = modelTrees.Values.FirstOrDefault(t => t.ModelType == entityTree.ModelType)
                         ?? modelTrees.Values.FirstOrDefault(t =>
                             t.Alias.Equals(childAlias, StringComparison.OrdinalIgnoreCase));
            if (result == null)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"[WARN] No model tree for entity '{entityTree.Alias}'. Skipping.");
                Console.ResetColor();
            }
            return result;
        }

        private static NodeTree GetRootFromWrapper<TWrapper>(
            Dictionary<string, NodeTree> modelTrees)
        {
            var wrapperType  = typeof(TWrapper);
            var rootListProp = wrapperType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p =>
                    p.PropertyType.IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(List<>));

            if (rootListProp == null)
                throw new InvalidOperationException($"{wrapperType.Name} has no List<T> root property");

            var rootModelType = rootListProp.PropertyType.GetGenericArguments()[0];
            return modelTrees.Values.FirstOrDefault(t => t.ModelType == rootModelType)
                   ?? throw new InvalidOperationException(
                       $"No NodeTree found for root model type '{rootModelType.Name}'");
        }

        private static PropertyInfo? ResolveNavigationProperty(
            Type parentType, Type childType, string prefix, string childAlias)
        {
            if (!string.IsNullOrWhiteSpace(childAlias))
            {
                var aliasProp = parentType.GetProperty(
                    childAlias, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (aliasProp != null) return aliasProp;
            }

            return parentType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .OrderByDescending(a =>
                    a.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) &&
                    a.Name.Matches(prefix))
                .FirstOrDefault(p =>
                {
                    if (p.PropertyType == childType) return true;
                    if (!p.PropertyType.IsGenericType) return false;
                    return p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
                           p.PropertyType.GetGenericArguments()[0] == childType;
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
                    .Where(t => t.EntityType?.Name.Equals(
                        entityType.Name, StringComparison.OrdinalIgnoreCase) == true)
                    .OrderBy(t => t.Id).ToList();

            return seen < candidates.Count ? candidates[seen] : candidates.FirstOrDefault();
        }

        private static Dictionary<int, string> BuildAliasIndex(
            Dictionary<string, Type> entityMapping)
        {
            var dict = new Dictionary<int, string>();
            int i = 0;
            foreach (var kv in entityMapping) dict[i++] = kv.Key;
            return dict;
        }
    }

    public class NodeResult
    {
        public object RootObject    { get; set; }
        public object CurrentObject { get; set; }

        public Dictionary<string, List<object>> RawObjects { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, object> Objects { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, List<object>> AllObjects { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public List<Type>     ModelTypes     { get; set; }
        public List<NodeTree> ModelNodeTrees { get; set; }
    }
}