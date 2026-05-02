using System.Collections.Concurrent;
using System.Reflection;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;

public class GraphMaterializer
{
    private readonly IMapper _mapper;

    public GraphMaterializer(IMapper mapper)
    {
        _mapper = mapper;
    }

    // =========================================================
    // ENTRY POINT
    // =========================================================
    public void MergeRow<M>(
        object[] map,
        SqlNode[] sqlNodes,
        Dictionary<string, NodeTree> trees,
        Dictionary<string, Type> entityMapping,
        Dictionary<string, M> edgeDict,
        ref int? totalCount,
        ref int? totalPageRecords)
        where M : class, new()
    {
        var slots = BuildSlots(map, sqlNodes);

        // Find the first non-null row
        var rootSlot = slots.FirstOrDefault(x => x.Value.row is not null and not DBNull);
        if (rootSlot.Key == null) return;

        var rootKey = BuildKey(rootSlot.Value.row, rootSlot.Value.node);

        if (!edgeDict.TryGetValue(rootKey, out var wrapper))
        {
            wrapper = new M();
            edgeDict[rootKey] = wrapper;
        }

        // Map each slot using correct alias
        foreach (var slot in slots)
        {
            var row = slot.Value.row;
            if (row is null or DBNull) continue;

            // Resolve alias properly using NodeTree and entityMapping
            var alias = ResolveAlias(slot.Value.node, entityMapping, trees);

            Map(row, wrapper, alias);
        }
    }

    // =========================================================
    // SLOT BUILDER (DAPPER POSITIONAL)
    // =========================================================
    private static Dictionary<string, (SqlNode node, object row)> BuildSlots(
        object[] map,
        SqlNode[] sqlNodes)
    {
        var slots = new Dictionary<string, (SqlNode, object)>();

        for (int i = 0; i < map.Length; i++)
        {
            if (i >= sqlNodes.Length) continue;
            slots[$"Id_{i}"] = (sqlNodes[i], map[i]);
        }

        return slots;
    }

    // =========================================================
    // RESOLVE ALIAS
    // =========================================================
    private string ResolveAlias(SqlNode node, Dictionary<string, Type> entityMapping, Dictionary<string, NodeTree> trees)
    {
        if (node == null) return "Unknown";

        // 1. Prefer entityMapping
        if (entityMapping != null && entityMapping.ContainsKey(node.Table))
            return node.Table;

        // 2. Use NodeTree structure
        if (trees != null)
        {
            var treeMatch = trees.Values.FirstOrDefault(t => t.Name == node.Table || t.Alias == node.Table);
            if (treeMatch != null)
                return treeMatch.Alias ?? treeMatch.Name ?? node.Table;
        }

        // 3. Fallback to table name
        return node.Table ?? "Unknown";
    }

    // =========================================================
    // MAP USING IMapper
    // =========================================================
    private void Map(object source, object target, string alias)
    {
        object mapped;
        try
        {
            mapped = _mapper.MapByAlias(source.GetType(), source, alias);
        }
        catch
        {
            return;
        }

        if (mapped == null) return;

        var targetType = target.GetType();

        foreach (var prop in mapped.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var value = prop.GetValue(mapped);
            if (value == null) continue;

            var targetProp = targetType.GetProperty(
                prop.Name,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);

            if (targetProp == null || !targetProp.CanWrite) continue;

            try
            {
                var t = Nullable.GetUnderlyingType(targetProp.PropertyType) ?? targetProp.PropertyType;

                var safeValue = t.IsAssignableFrom(value.GetType())
                    ? value
                    : Convert.ChangeType(value, t);

                targetProp.SetValue(target, safeValue);
            }
            catch
            {
                // ignore conversion safely
            }
        }
    }

    // =========================================================
    // BUILD KEY
    // =========================================================
    private static string BuildKey(object obj, SqlNode node)
    {
        if (node.UpsertKeys == null || node.UpsertKeys.Count == 0)
            return obj.GetHashCode().ToString();

        var key = node.Table;

        foreach (var k in node.UpsertKeys)
        {
            var prop = obj.GetType().GetProperty(k);
            if (prop == null) continue;

            var val = prop.GetValue(obj);
            if (val != null)
                key += "|" + val;
        }

        return key;
    }
}