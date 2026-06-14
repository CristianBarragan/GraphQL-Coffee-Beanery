using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.Sql;
using Npgsql;
using System.Collections;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.GraphQL.Helper;

namespace CoffeeBeanery.Service
{
    public class QueryHandler<M> : ProcessQuery<M>,
        IQuery<ProcessQueryParameters,
            (List<M> list, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>
        where M : class, new()
    {
        private readonly IMapper _mapper;

        public QueryHandler(
            ILoggerFactory loggerFactory,
            NpgsqlDataSource dataSource,
            IMapper mapper)
            : base(loggerFactory, dataSource)
        {
            _mapper = mapper;
        }

        // Called by QueryEngine after SQL execution — receives pre-fetched rows
        public async Task<(List<M> list, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>
            HydrateAsync(
                IList<object[]> rowMatrix,
                ProcessQueryParameters input,
                CancellationToken ct)
        {
            int totalCount  = 0;
            int pageRecords = 0;

            var graph      = input.Context.Graph;
            var aliasIndex = BuildAliasIndex(input.Context.SplitOnDapper);
            var rootNode   = GetRootNode<M>(graph);

            var rootAliasIndex = aliasIndex
                .FirstOrDefault(kv =>
                    kv.Value.Equals(rootNode.Alias, StringComparison.OrdinalIgnoreCase))
                .Key;

            var rowsByRootKey = new Dictionary<Guid, List<object[]>>();
            var rootKeyOrder  = new List<Guid>();
            var seenCounts    = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            // Group rows by root entity key
            foreach (var row in rowMatrix)
            {
                if (row[rootAliasIndex] is TotalPageRecords tp) { pageRecords = tp.PageRecords; continue; }
                if (row[rootAliasIndex] is TotalRecordCount tr) { totalCount  = tr.RecordCount;  continue; }

                var rootEntity = row[rootAliasIndex];
                if (rootEntity == null) continue;

                var keyProp = FindKeyProperty(rootEntity.GetType());
                if (keyProp?.GetValue(rootEntity) is not Guid rootKey || rootKey == Guid.Empty) continue;

                if (!rowsByRootKey.ContainsKey(rootKey))
                {
                    rowsByRootKey[rootKey] = new List<object[]>();
                    rootKeyOrder.Add(rootKey);
                }

                rowsByRootKey[rootKey].Add(row);
            }

            var wrappers = new List<M>();

            foreach (var rootKey in rootKeyOrder)
            {
                var groupRows  = rowsByRootKey[rootKey];
                var rawByAlias = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase);
                seenCounts.Clear();

                foreach (var row in groupRows)
                {
                    seenCounts.Clear();

                    for (int i = 0; i < row.Length; i++)
                    {
                        if (row[i] == null) continue;
                        if (row[i] is TotalPageRecords || row[i] is TotalRecordCount) continue;

                        var alias = aliasIndex.TryGetValue(i, out var a) ? a : row[i].GetType().Name;

                        if (!graph.Nodes.TryGetValue(alias, out _)) continue;

                        if (!rawByAlias.TryGetValue(alias, out var bucket))
                        {
                            bucket = new List<object>();
                            rawByAlias[alias] = bucket;
                        }

                        bucket.Add(row[i]);
                    }
                }

                var dedupedRaw = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase);
                foreach (var (alias, bucket) in rawByAlias)
                    dedupedRaw[alias] = DeduplicateByKey(bucket);

                var rootObject = Activator.CreateInstance<M>();

                var nodeResult = new NodeResult
                {
                    RootObject    = rootObject,
                    CurrentObject = rootObject,
                    RawObjects    = dedupedRaw,
                    Objects       = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase),
                    AllObjects    = new Dictionary<string, List<object>>(StringComparer.OrdinalIgnoreCase),
                };

                Build(rootNode, graph, nodeResult);

                var wrapper = (M)nodeResult.RootObject;

                if (nodeResult.Objects.TryGetValue(rootNode.Alias, out var rootObj))
                    Attach(wrapper, rootObj, nodeResult);

                wrappers.Add(wrapper);
            }

            return (
                wrappers,
                input.Context.PaginationContext?.StartCursor,
                input.Context.PaginationContext?.EndCursor,
                totalCount,
                pageRecords);
        }

        // IQuery entry point — executes SQL then hydrates (used outside QueryEngine if needed)
        public async Task<(List<M> list, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>
            ExecuteAsync(ProcessQueryParameters input, CancellationToken ct)
        {
            var rowMatrix = await base.ExecuteAsync(input, ct);
            return await HydrateAsync(rowMatrix, input, ct);
        }

        // =========================
        // Mapping
        // =========================

        private List<object> MapAlias(string alias, NodeResult nodeResult)
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

        // =========================
        // Tree walking
        // =========================

        private void Build(
            GraphILNode node,
            GraphIL graph,
            NodeResult nodeResult)
        {
            if (!nodeResult.Objects.TryGetValue(node.Alias, out var currentModel))
            {
                var mappedList = MapAlias(node.Alias, nodeResult);

                if (mappedList.Count > 0)
                {
                    currentModel = mappedList[0];
                    nodeResult.Objects[node.Alias]    = currentModel;
                    nodeResult.AllObjects[node.Alias] = mappedList;
                }
                else
                {
                    currentModel = Activator.CreateInstance(node.EntityType)!;
                    nodeResult.Objects[node.Alias] = currentModel;
                }
            }

            nodeResult.CurrentObject = currentModel!;

            // Walk outbound edges from this node
            if (!graph.EdgesBySourceAlias.TryGetValue(node.Alias, out var edges))
                return;

            foreach (var edge in edges)
            {
                if (!graph.Nodes.TryGetValue(edge.ToAlias, out var childNode)) continue;

                Build(childNode, graph, nodeResult);

                if (nodeResult.Objects.TryGetValue(childNode.Alias, out var childObj))
                    Attach(currentModel, childObj, nodeResult, childAlias: childNode.Alias);
            }

            ProjectAggregates(nodeResult, graph);
        }

        // =========================
        // Helpers
        // =========================

        private static GraphILNode GetRootNode<TWrapper>(GraphIL graph)
        {
            var wrapperType  = typeof(TWrapper);
            var rootListProp = wrapperType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p =>
                    p.PropertyType.IsGenericType &&
                    p.PropertyType.GetGenericTypeDefinition() == typeof(List<>));

            if (rootListProp == null)
                throw new InvalidOperationException(
                    $"{wrapperType.Name} has no List<T> root property");

            var rootEntityType = rootListProp.PropertyType.GetGenericArguments()[0];

            return graph.Nodes.Values.FirstOrDefault(n => n.EntityType == rootEntityType)
                   ?? throw new InvalidOperationException(
                       $"No GraphILNode found for entity type '{rootEntityType.Name}'");
        }

        private static List<object> DeduplicateByKey(List<object> items)
        {
            if (items.Count == 0) return items;

            var keyProp = FindKeyProperty(items[0].GetType());
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

        private static PropertyInfo? FindKeyProperty(Type type) =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p =>
                    (p.PropertyType == typeof(Guid) || p.PropertyType == typeof(Guid?)) &&
                    p.Name.EndsWith("Key", StringComparison.OrdinalIgnoreCase));

        private static void ProjectAggregates(NodeResult nodeResult, GraphIL graph)
        {
            foreach (var node in graph.Nodes.Values)
            {
                if (!nodeResult.Objects.TryGetValue(node.Alias, out var targetModel)) continue;

                var targetType = targetModel.GetType();

                foreach (var field in node.Fields)
                {
                    // Fields that reference another alias carry a dot-separated DestinationName
                    var parts = field.DestinationName.Split('.');
                    if (parts.Length < 2) continue;

                    var sourceAlias = parts[0];
                    var sourcePropName = parts[^1];

                    if (!nodeResult.Objects.TryGetValue(sourceAlias, out var sourceObj)) continue;

                    var sourceProp = sourceObj.GetType().GetProperty(sourcePropName);
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

                var keyProp = FindKeyProperty(childType);

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

        private static void SafeSet(PropertyInfo prop, object instance, object? value)
        {
            if (prop == null || value == null) return;
            var targetType = prop.PropertyType;

            if (targetType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(targetType))
            {
                var elementType = targetType.GetGenericArguments()[0];
                var existing    = prop.GetValue(instance);
                if (existing == null)
                {
                    existing = Activator.CreateInstance(targetType);
                    prop.SetValue(instance, existing);
                }
                var list = (IList)existing!;

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

        private static PropertyInfo? ResolveNavigationProperty(
            Type parentType, Type childType, string prefix, string childAlias)
        {
            if (!string.IsNullOrWhiteSpace(childAlias))
            {
                var aliasProp = parentType.GetProperty(
                    childAlias,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
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

        private static Dictionary<int, string> BuildAliasIndex(
            Dictionary<string, Type> splitOnDapper)
        {
            var dict = new Dictionary<int, string>();
            int i    = 0;
            foreach (var kv in splitOnDapper)
            {
                var alias = kv.Key.Contains('~')
                    ? kv.Key.Split('~')[0]
                    : kv.Value?.Name ?? kv.Key;
                dict[i++] = alias;
            }
            return dict;
        }
    }

    public class NodeResult
    {
        public object RootObject    { get; set; } = null!;
        public object CurrentObject { get; set; } = null!;

        public Dictionary<string, List<object>> RawObjects { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, object> Objects { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, List<object>> AllObjects { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);
    }
}