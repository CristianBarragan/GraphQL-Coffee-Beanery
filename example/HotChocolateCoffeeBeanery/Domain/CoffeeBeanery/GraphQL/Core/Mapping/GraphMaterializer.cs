using System.Linq;
using System.Reflection;
using System.Text;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;

public static class GraphMaterializer
{
    // =========================
    // PUBLIC ENTRY
    // =========================

    public static void MergeRow<M>(
        object[] map,
        SqlNode[] sqlNodes,
        Dictionary<string, NodeTree> trees,
        Dictionary<string, Type> entityMapping,
        Dictionary<string, Action<object, object>> mapper,
        Dictionary<string, M> edgeDict,
        ref int? totalCount,
        ref int? totalPageRecords)
        where M : class, new()
    {
        string? rootKey = null;

        for (int i = 0; i < map.Length && i < sqlNodes.Length; i++)
        {
            if (map[i] is null or DBNull) continue;
            if (TryHandleMeta(map[i], ref totalCount, ref totalPageRecords)) continue;

            var node = sqlNodes[i];
            if (node is null) continue;

            rootKey = BuildCompositeKey(map[i], node);
            break;
        }

        if (rootKey is null) return;

        if (!edgeDict.TryGetValue(rootKey, out var edge))
        {
            edge = new M();
            edgeDict[rootKey] = edge;
        }

        // 🔥 ROOT NODES ONLY
        foreach (var rootTree in trees.Values.Where(t => t.IsGraph))
        {
            ApplyTree(
                rootTree,
                map,
                sqlNodes,
                trees,
                entityMapping,
                mapper,
                edge!);
        }
    }

    // =========================
    // 🔥 RECURSIVE TREE WALK
    // =========================

    private static void ApplyTree(
        NodeTree tree,
        object[] map,
        SqlNode[] sqlNodes,
        Dictionary<string, NodeTree> trees,
        Dictionary<string, Type> entityMapping,
        Dictionary<string, Action<object, object>> mapper,
        object edge)
    {
        foreach (var field in tree.Mapping)
        {
            // find matching column in SQL row
            for (int i = 0; i < map.Length && i < sqlNodes.Length; i++)
            {
                if (map[i] is null or DBNull) continue;

                var node = sqlNodes[i];
                if (node is null) continue;

                if (!string.Equals(field.SourceName, node.Table, StringComparison.OrdinalIgnoreCase))
                    continue;

                var key = Norm(field.DestinationEntity);

                if (mapper.TryGetValue(key, out var action))
                    action(edge, map[i]);
            }
        }

        // 🔥 RECURSE INTO CHILDREN
        foreach (var child in tree.Children)
        {
            if (!trees.TryGetValue(child.To, out var childTree))
                continue;

            ApplyTree(
                childTree,
                map,
                sqlNodes,
                trees,
                entityMapping,
                mapper,
                edge);
        }
    }

    // =========================
    // BUILD MAPPER
    // =========================

    public static Dictionary<string, Action<object, object>> BuildMapper<M>(
        SqlNode[] nodes,
        Dictionary<string, NodeTree> trees,
        Dictionary<string, Type> entityMapping)
    {
        var result = new Dictionary<string, Action<object, object>>();
        var edgeType = typeof(M);
        var props = edgeType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var tree in trees.Values)
        {
            foreach (var field in tree.Mapping)
            {
                var key = Norm(field.DestinationEntity);

                if (!entityMapping.TryGetValue(key, out var targetType))
                    continue;

                var prop = props.FirstOrDefault(p =>
                    p.CanWrite &&
                    (
                        p.PropertyType == targetType ||
                        (IsCollectionProperty(p.PropertyType, out var el)
                            && el == targetType)
                    ));

                if (prop is null)
                    continue;

                result[key] = BuildSetter(prop);
            }
        }

        return result;
    }

    // =========================
    // SETTER
    // =========================

    private static Action<object, object> BuildSetter(PropertyInfo prop)
    {
        var propType = prop.PropertyType;

        if (IsCollectionProperty(propType, out var elementType))
        {
            return (edge, value) =>
            {
                if (value is null) return;

                var model = GetModel(edge);
                if (model is null) return;

                if (elementType != null && elementType != value.GetType())
                    return;

                var collection = prop.GetValue(model);

                if (collection is null)
                {
                    collection = Activator.CreateInstance(propType);
                    prop.SetValue(model, collection);
                }

                propType.GetMethod("Add")?.Invoke(collection, new[] { value });
            };
        }

        return (edge, value) =>
        {
            if (value is null) return;

            var model = GetModel(edge);
            if (model is null) return;

            if (prop.PropertyType == value.GetType())
                prop.SetValue(model, value);
        };
    }

    // =========================
    // WRAPPER UNWRAP
    // =========================

    private static object? GetModel(object edge)
        => edge.GetType().GetProperty("Model")?.GetValue(edge);

    // =========================
    // KEY BUILDING
    // =========================

    private static string BuildCompositeKey(object mapped, SqlNode node)
    {
        if (node.UpsertKeys.Count == 0)
            return $"{node.Table}::fallback::{mapped.GetHashCode()}";

        var sb = new StringBuilder(node.Table).Append("::");

        foreach (var keyCol in node.UpsertKeys)
        {
            var prop = mapped.GetType()
                .GetProperty(keyCol, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            var val = prop?.GetValue(mapped);
            if (val is null) continue;

            sb.Append(keyCol).Append('=').Append(val).Append('|');
        }

        return sb.ToString();
    }

    // =========================
    // HELPERS
    // =========================

    private static bool IsCollectionProperty(Type type, out Type? elementType)
    {
        elementType = null;

        if (type == typeof(string)) return false;

        if (type.IsGenericType)
        {
            var def = type.GetGenericTypeDefinition();

            if (def == typeof(List<>)
                || def == typeof(IList<>)
                || def == typeof(ICollection<>)
                || def == typeof(IEnumerable<>))
            {
                elementType = type.GetGenericArguments()[0];
                return true;
            }
        }

        return false;
    }

    private static bool TryHandleMeta(
        object item,
        ref int? totalCount,
        ref int? totalPageRecords)
    {
        if (item is TotalPageRecords tpr)
        {
            totalPageRecords = tpr.PageRecords;
            return true;
        }

        if (item is TotalRecordCount trc)
        {
            totalCount = trc.RecordCount;
            return true;
        }

        return false;
    }

    private static string Norm(string s)
        => s?.Trim().TrimEnd(':');
}