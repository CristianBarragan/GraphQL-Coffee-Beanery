using System.Collections.Concurrent;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;

public static class GraphMaterializer
{
    // Per-root-Wrapper cache: compositeKey → child instance.
    // ConditionalWeakTable ties lifetime to the root — no manual cleanup.
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<
        object,
        Dictionary<string, object>
    > _nodeCache = new();

    // Reflection cache — paid once per (Type, propertyName) across all rows.
    private static readonly ConcurrentDictionary<(Type, string), PropertyInfo?>
        _propCache = new();

    // =========================================================
    // PUBLIC ENTRY
    // =========================================================
    public static void MergeRow<M>(
        object[]                     map,
        SqlNode[]                    sqlNodes,
        Dictionary<string, NodeTree> trees,
        Dictionary<string, Type>     entityMapping,  // insertion order = map[] index
        Dictionary<string, M>        edgeDict,
        string                       model,          // e.g. "CustomerCustomerEdge"
        ref int?                     totalCount,
        ref int?                     totalPageRecords)
        where M : class, new()
    {
        SqlNode? rootNode = null;
        object?  rootRow  = null;

        for (int i = 0; i < map.Length && i < sqlNodes.Length; i++)
        {
            if (map[i] is null or DBNull) continue;
            if (TryHandleMeta(map[i], ref totalCount, ref totalPageRecords)) continue;
            rootNode = sqlNodes[i];
            rootRow  = map[i];
            break;
        }

        if (rootNode is null || rootRow is null) return;

        var rootKey = BuildCompositeKey(rootRow, rootNode);
        if (rootKey is null) return;

        // Get-or-create the root Wrapper (M).
        if (!edgeDict.TryGetValue(rootKey, out var wrapper))
        {
            wrapper = new M();
            edgeDict[rootKey] = wrapper;
        }

        // Build alias-keyed slot lookup.
        // EntityMapping insertion order matches map[] index exactly.
        var slots = BuildSlots(map, sqlNodes, entityMapping);

        // Root tree = first key in EntityMapping (always the root entity alias).
        // Do NOT use IsGraph — that flag is reserved for Apache AGE graph models.
        var rootAlias = entityMapping.Keys.First();
        if (!trees.TryGetValue(rootAlias, out var rootTree)) return;

        if (!slots.TryGetValue(rootAlias, out var rootSlot)) return;
        var (_, rootRowObj) = rootSlot;
        if (rootRowObj is null or DBNull) return;

        // Find or create the model edge element on Wrapper
        // e.g. Wrapper.CustomerCustomerEdge[n] (List<CustomerCustomerEdge>).
        var edgeElement = GetOrCreateEdgeElement(wrapper!, rootKey, model, rootRow, rootNode);
        if (edgeElement is null) return;

        // Map root tree scalar fields onto the edge element.
        MapFields(rootTree, rootRowObj, edgeElement);

        // Create scalar children whose data lives in the root row
        // (e.g. CustomerCustomerRelationship shares slot 0 with the edge).
        CreateScalarChildrenFromRootRow(edgeElement, rootTree, rootRowObj, trees, wrapper!);

        // Recurse into children (InnerCustomer, OuterCustomer, etc.).
        Recurse(rootTree, slots, trees, edgeElement, wrapper!);
    }

    // =========================================================
    // GET OR CREATE EDGE ELEMENT
    //
    // Finds the List<T> property on Wrapper whose element type name
    // matches `model`, then gets or creates the element for this row.
    // =========================================================
    private static object? GetOrCreateEdgeElement(
        object  wrapper,
        string  rootKey,
        string  model,
        object  rootRow,
        SqlNode rootNode)
    {
        PropertyInfo? listProp = null;

        foreach (var prop in wrapper.GetType()
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            var elType = GetCollectionElementType(prop.PropertyType);
            if (elType is null) continue;

            if (string.Equals(elType.Name, model, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(prop.Name,   model, StringComparison.OrdinalIgnoreCase))
            {
                listProp = prop;
                break;
            }
        }

        // No list property — Wrapper itself is flat, use it directly.
        if (listProp is null)
            return wrapper;

        var cache    = _nodeCache.GetOrCreateValue(wrapper);
        var cacheKey = $"{model}::{rootKey}";

        if (cache.TryGetValue(cacheKey, out var existing))
            return existing;

        var elementType = GetCollectionElementType(listProp.PropertyType)!;
        object element;
        try   { element = Activator.CreateInstance(elementType)!; }
        catch { return null; }

        cache[cacheKey] = element;

        var list = listProp.GetValue(wrapper);
        if (list is null)
        {
            list = Activator.CreateInstance(listProp.PropertyType)!;
            listProp.SetValue(wrapper, list);
        }
        listProp.PropertyType.GetMethod("Add")?.Invoke(list, new[] { element });

        return element;
    }

    // =========================================================
    // CREATE SCALAR CHILDREN FROM ROOT ROW
    //
    // Some scalar properties on the edge element (e.g.
    // CustomerCustomerEdge.CustomerCustomerRelationship) share the
    // root Dapper row rather than having their own JOIN slot.
    // Find them by matching property type name to a NodeTree key,
    // create them, map fields from root row, and attach.
    // =========================================================
    private static void CreateScalarChildrenFromRootRow(
        object                       edgeElement,
        NodeTree                     rootTree,
        object                       rootRowObj,
        Dictionary<string, NodeTree> trees,
        object                       root)
    {
        foreach (var prop in edgeElement.GetType()
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanWrite)                                    continue;
            if (prop.PropertyType.IsPrimitive)                    continue;
            if (prop.PropertyType == typeof(string))               continue;
            if (prop.PropertyType.IsEnum)                          continue;
            if (GetCollectionElementType(prop.PropertyType) != null) continue;
            if (prop.GetValue(edgeElement) is not null)            continue;

            // Nullable<T> unwrap (e.g. Guid?)
            var underlying = Nullable.GetUnderlyingType(prop.PropertyType);
            if (underlying != null) continue;

            var typeName = prop.PropertyType.Name;

            if (!trees.TryGetValue(typeName,  out var childTree) &&
                !trees.TryGetValue(prop.Name, out childTree))
                continue;

            object childObj;
            try   { childObj = Activator.CreateInstance(prop.PropertyType)!; }
            catch { continue; }

            MapFields(childTree, rootRowObj, childObj);

            // Cache so Recurse doesn't create a duplicate.
            var compositeKey = $"{childTree.Alias ?? childTree.Name}::scalar::{rootRowObj.GetHashCode()}";
            var cache        = _nodeCache.GetOrCreateValue(root);
            cache.TryAdd(compositeKey, childObj);

            prop.SetValue(edgeElement, childObj);
        }
    }

    // =========================================================
    // SCALAR FIELD MAPPING
    //
    // FieldMap direction (as registered in mappings):
    //   SourceName      = model property name
    //   DestinationName = entity property name
    //
    // Materializer reads from entity row → writes to domain model:
    //   read  using DestinationName (entity)  with SourceName as fallback
    //   write using SourceName      (model)   with DestinationName as fallback
    // =========================================================
    private static void MapFields(NodeTree tree, object rowObj, object target)
    {
        foreach (var field in tree.Mapping)
        {
            var src = GetProp(rowObj.GetType(), field.DestinationName)
                      ?? GetProp(rowObj.GetType(), field.SourceName);
            if (src is null) continue;

            var val = src.GetValue(rowObj);
            if (val is null) continue;

            var dst = GetProp(target.GetType(), field.SourceName)
                      ?? GetProp(target.GetType(), field.DestinationName);
            if (dst is null || !dst.CanWrite) continue;

            SetSafe(target, dst, val);
        }
    }

    // =========================================================
    // RECURSIVE CHILD WALK
    // =========================================================
    private static void Recurse(
        NodeTree                              tree,
        Dictionary<string, (SqlNode, object)> slots,
        Dictionary<string, NodeTree>          trees,
        object                                parent,
        object                                root)
    {
        foreach (var link in tree.Children.Concat(tree.RelatedChildren))
        {
            if (!trees.TryGetValue(link.To, out var childTree)) continue;

            // Alias is authoritative — Name may be the model type name
            // ("Customer") rather than the join alias ("InnerCustomer").
            var effectiveAlias = !string.IsNullOrEmpty(childTree.Alias)
                ? childTree.Alias
                : childTree.Name;

            if (!slots.TryGetValue(effectiveAlias, out var childSlot) &&
                !slots.TryGetValue(childTree.Name,  out childSlot))
                continue;

            var (childNode, childRow) = childSlot;
            if (childRow is null or DBNull) continue;

            // link.To is the exact property name on parent
            // (e.g. "InnerCustomer", "OuterCustomer") — use it directly
            // to avoid ambiguity when multiple properties share the same type.
            var child = ResolveOrCreate(
                parent, root, childTree, childNode, childRow,
                propertyName: link.To,
                cacheAlias:   effectiveAlias);
            if (child is null) continue;

            MapFields(childTree, childRow, child);

            // Copy FK value from parent onto child via LinkKey.
            // e.g. parent.InnerCustomerKey → child.CustomerKey
            // link.FromColumn = source property on parent
            // link.ToColumn   = destination property on child
            CopyLinkKey(parent, child, link);

            Recurse(childTree, slots, trees, child, root);
        }
    }

    // =========================================================
    // COPY LINK KEY
    //
    // Copies the FK value declared in a LinkKey from parent to child.
    // Used when the child's JOIN slot does not re-select the key column
    // (e.g. InnerCustomer slot only selects Id, not CustomerKey —
    // but CustomerKey = InnerCustomerKey already set on the parent edge).
    // =========================================================
    private static void CopyLinkKey(object parent, object child, LinkKey link)
    {
        if (string.IsNullOrEmpty(link.FromColumn) ||
            string.IsNullOrEmpty(link.ToColumn)) return;

        var srcProp = GetProp(parent.GetType(), link.FromColumn);
        if (srcProp is null) return;

        var val = srcProp.GetValue(parent);
        if (val is null) return;

        var dstProp = GetProp(child.GetType(), link.ToColumn);
        if (dstProp is null || !dstProp.CanWrite) return;

        SetSafe(child, dstProp, val);
    }

    // =========================================================
    // RESOLVE-OR-CREATE  (dedup child objects per root)
    // =========================================================
    private static object? ResolveOrCreate(
        object   parent,
        object   root,
        NodeTree childTree,
        SqlNode  childNode,
        object   childRowObj,
        string?  propertyName = null,
        string?  cacheAlias   = null)
    {
        var alias        = cacheAlias ?? childTree.Alias ?? childTree.Name;
        var compositeKey = $"{alias}::{BuildCompositeKey(childRowObj, childNode)}";
        var cache        = _nodeCache.GetOrCreateValue(root);

        if (cache.TryGetValue(compositeKey, out var existing))
            return existing;

        // Use link.To (propertyName) as primary lookup — unambiguous even when
        // multiple properties share the same type (InnerCustomer / OuterCustomer).
        var lookupName = propertyName ?? alias;
        var prop       = FindChildProp(parent.GetType(), lookupName, alias);

        if (prop is null)
        {
            // No matching child property — parent itself is the target.
            cache[compositeKey] = parent;
            return parent;
        }

        var childType = GetCollectionElementType(prop.PropertyType) ?? prop.PropertyType;
        object childObj;
        try   { childObj = Activator.CreateInstance(childType)!; }
        catch { return null; }

        cache[compositeKey] = childObj;
        Attach(parent, prop, childObj);
        return childObj;
    }

    // =========================================================
    // PROPERTY SEARCH
    //
    // Pass 1: exact property name match (handles InnerCustomer vs
    //         OuterCustomer which are both of type Customer).
    // Pass 2: element type name match (for unambiguous cases like
    //         List<CustomerBankingRelationship>).
    // =========================================================
    private static PropertyInfo? FindChildProp(Type parentType, string name, string alias)
    {
        var props = parentType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Pass 1 — exact property name
        foreach (var prop in props)
        {
            if (!prop.CanWrite && GetCollectionElementType(prop.PropertyType) is null)
                continue;

            if (string.Equals(prop.Name, name,  StringComparison.OrdinalIgnoreCase) ||
                string.Equals(prop.Name, alias, StringComparison.OrdinalIgnoreCase))
                return prop;
        }

        // Pass 2 — element type name (fallback for unambiguous cases)
        foreach (var prop in props)
        {
            if (!prop.CanWrite && GetCollectionElementType(prop.PropertyType) is null)
                continue;

            var elType = GetCollectionElementType(prop.PropertyType) ?? prop.PropertyType;

            if (string.Equals(elType.Name, name,  StringComparison.OrdinalIgnoreCase) ||
                string.Equals(elType.Name, alias, StringComparison.OrdinalIgnoreCase))
                return prop;
        }

        return null;
    }

    private static void Attach(object parent, PropertyInfo prop, object child)
    {
        if (GetCollectionElementType(prop.PropertyType) is not null)
        {
            var list = prop.GetValue(parent);
            if (list is null)
            {
                list = Activator.CreateInstance(prop.PropertyType)!;
                prop.SetValue(parent, list);
            }
            prop.PropertyType.GetMethod("Add")?.Invoke(list, new[] { child });
        }
        else if (prop.CanWrite)
        {
            prop.SetValue(parent, child);
        }
    }

    // =========================================================
    // BUILD SLOTS
    //
    // EntityMapping insertion order matches map[] index exactly.
    // Key = alias (e.g. "InnerCustomer", "OuterCustomer").
    // =========================================================
    private static Dictionary<string, (SqlNode node, object row)> BuildSlots(
        object[]                 map,
        SqlNode[]                sqlNodes,
        Dictionary<string, Type> entityMapping)
    {
        var slots   = new Dictionary<string, (SqlNode, object)>(
            StringComparer.OrdinalIgnoreCase);
        var aliases = entityMapping.Keys.ToList();

        for (int i = 0; i < aliases.Count && i < map.Length; i++)
        {
            if (map[i] is null or DBNull)                    continue;
            if (i >= sqlNodes.Length || sqlNodes[i] is null) continue;

            var alias = aliases[i];
            if (!slots.ContainsKey(alias))
                slots[alias] = (sqlNodes[i], map[i]);
        }

        return slots;
    }

    // =========================================================
    // BUILD MAPPER — no-op, kept for any remaining call sites
    // =========================================================
    public static Dictionary<string, Action<object, object>> BuildMapper<M>(
        SqlNode[]                    nodes,
        Dictionary<string, NodeTree> trees,
        Dictionary<string, Type>     entityMapping)
        => new();

    // =========================================================
    // UTILITIES
    // =========================================================
    private static PropertyInfo? GetProp(Type type, string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return _propCache.GetOrAdd(
            (type, name),
            k => k.Item1.GetProperty(
                k.Item2,
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    private static void SetSafe(object target, PropertyInfo prop, object value)
    {
        try
        {
            var underlying = Nullable.GetUnderlyingType(prop.PropertyType)
                             ?? prop.PropertyType;
            prop.SetValue(target,
                underlying == value.GetType()
                    ? value
                    : Convert.ChangeType(value, underlying));
        }
        catch { }
    }

    private static string BuildCompositeKey(object obj, SqlNode node)
    {
        if (node.UpsertKeys.Count == 0)
            return $"{node.Table}::fallback::{obj.GetHashCode()}";

        var sb = new System.Text.StringBuilder(node.Table).Append("::");
        foreach (var keyCol in node.UpsertKeys)
        {
            var val = GetProp(obj.GetType(), keyCol)?.GetValue(obj);
            if (val is null) continue;
            sb.Append(keyCol).Append('=').Append(val).Append('|');
        }
        return sb.ToString();
    }

    private static Type? GetCollectionElementType(Type type)
    {
        if (type == typeof(string) || !type.IsGenericType) return null;
        var def = type.GetGenericTypeDefinition();
        return (def == typeof(List<>)
             || def == typeof(IList<>)
             || def == typeof(ICollection<>)
             || def == typeof(IEnumerable<>))
            ? type.GetGenericArguments()[0]
            : null;
    }

    private static bool TryHandleMeta(
        object item, ref int? totalCount, ref int? totalPageRecords)
    {
        if (item is TotalPageRecords tpr) { totalPageRecords = tpr.PageRecords; return true; }
        if (item is TotalRecordCount trc) { totalCount       = trc.RecordCount; return true; }
        return false;
    }
}