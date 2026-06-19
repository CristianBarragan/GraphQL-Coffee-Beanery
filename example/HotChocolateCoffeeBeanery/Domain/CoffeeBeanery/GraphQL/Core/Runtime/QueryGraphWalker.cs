using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    /// <summary>
    /// Read-side counterpart to MutationGraphWalker. Walks the GraphQL selection set once and
    /// produces, per entity alias *instance* (RowKey - see below), the ordered list of columns
    /// that need to be SELECTed - resolved directly from NodeRegistry's compiled index
    /// (FrozenChildAliasByField to descend into nested selections, TryResolveLeafField to
    /// resolve scalar leaves to one or more (entityAlias, column) pairs).
    ///
    /// SELF-JOIN / REPEATED-ALIAS FIX: two distinct selection fields can resolve to the same
    /// NodeRegistry entity alias (e.g. `innerCustomer` and `outerCustomer` both backed by the
    /// "Customer" entity in a self-referencing relationship edge - NodeRegistry.ChildAliasByField
    /// already carries this as two separate (ParentAlias, FieldName) entries, both pointing at
    /// alias "Customer"). Every visited node is therefore identified by a RowKey: the field
    /// path that led to it (e.g. "Customer.customerCustomerEdge.innerCustomer"), not the bare
    /// alias. Result now also records, per RowKey, its ParentRowKey and the FieldName that
    /// produced it from that parent - SqlSelectStatementBuilder needs this to pick the correct
    /// EntityKey (matched by EntityKey.AliasProperty == FieldName) when two EntityKeys in
    /// EntityChildren/EntityChildrenRelated share the same AliasTo, so it can emit two distinct
    /// joined+aliased copies of the same table instead of collapsing/dropping one.
    /// </summary>
    public static class QueryGraphWalker
    {
        public sealed class Result
        {
            // rowKey -> ordered column names to select, insertion order preserved (PK first).
            public Dictionary<string, List<string>> ColumnsByRowKey { get; } =
                new(StringComparer.OrdinalIgnoreCase);

            // rowKey -> the real NodeRegistry entity alias backing it (for SQL table lookup).
            // Multiple rowKeys can map to the same Alias (self-join case).
            public Dictionary<string, string> AliasByRowKey { get; } =
                new(StringComparer.OrdinalIgnoreCase);

            // rowKey -> parent rowKey (null/"" for the root). Lets the SQL builder walk the
            // same instance tree the GraphQL selection actually described.
            public Dictionary<string, string> ParentRowKeyByRowKey { get; } =
                new(StringComparer.OrdinalIgnoreCase);

            // rowKey -> the GraphQL field name (off its parent) that produced this instance.
            // Empty for the root. This is what's matched against EntityKey.AliasProperty.
            public Dictionary<string, string> FieldNameByRowKey { get; } =
                new(StringComparer.OrdinalIgnoreCase);

            // rowKeys in the order first encountered - root first, then discovery order of
            // nested selections. Seeds traversal order and Dapper multi-mapping order.
            public List<string> RowKeyOrder { get; } = new();

            public void EnsureRow(string rowKey, string alias, string parentRowKey, string fieldName)
            {
                if (AliasByRowKey.ContainsKey(rowKey))
                    return;

                AliasByRowKey[rowKey] = alias;
                ParentRowKeyByRowKey[rowKey] = parentRowKey ?? "";
                FieldNameByRowKey[rowKey] = fieldName ?? "";
                ColumnsByRowKey[rowKey] = new List<string>();
                RowKeyOrder.Add(rowKey);
            }

            public void AddColumn(string rowKey, string alias, string column)
            {
                // alias/parentRowKey/fieldName already set by EnsureRow for every rowKey this
                // is called for (leaf resolution always happens after the owning row exists).
                if (!ColumnsByRowKey.TryGetValue(rowKey, out var list))
                {
                    list = new List<string>();
                    ColumnsByRowKey[rowKey] = list;
                    if (!AliasByRowKey.ContainsKey(rowKey))
                    {
                        AliasByRowKey[rowKey] = alias;
                        ParentRowKeyByRowKey[rowKey] = "";
                        FieldNameByRowKey[rowKey] = "";
                    }
                    RowKeyOrder.Add(rowKey);
                }

                if (!list.Any(c => string.Equals(c, column, StringComparison.OrdinalIgnoreCase)))
                    list.Add(column);
            }
        }

        public static Result Walk(string rootAlias, SelectionSetNode? selectionSet)
        {
            var result = new Result();
            result.EnsureRow(rootAlias, rootAlias, "", "");
            EnsurePrimaryKey(rootAlias, rootAlias, result);

            if (selectionSet is not null)
                WalkSelectionSet(rootAlias, rootAlias, selectionSet, result);

            return result;
        }

        /// <summary>
        /// alias = current NodeRegistry entity alias (used for FrozenChildAliasByField /
        /// FrozenEntityTrees / TryResolveLeafField lookups - "what SQL table are we on").
        /// rowKey = current instance path (keys Result's dictionaries so sibling selections
        /// resolving to the same alias - innerCustomer/outerCustomer - stay distinct).
        /// </summary>
        private static void WalkSelectionSet(string alias, string rowKey, SelectionSetNode selectionSet, Result result)
        {
            foreach (var selection in selectionSet.Selections)
            {
                if (selection is not FieldNode field)
                    continue;

                var fieldName = field.Name.Value;

                if (NodeRegistry.FrozenChildAliasByField.TryGetValue((alias, fieldName), out var childAlias))
                {
                    var childIsRealEntity = NodeRegistry.FrozenEntityTrees.ContainsKey(childAlias);

                    // Wrapper/passthrough fields (edges/node/items with no NodeRegistry
                    // mapping of their own) extend the rowKey path but never become rows
                    // themselves - only real entities get EnsureRow'd as instances.
                    var childRowKey = $"{rowKey}.{fieldName}";

                    if (childIsRealEntity)
                    {
                        result.EnsureRow(childRowKey, childAlias, rowKey, fieldName);
                        EnsurePrimaryKey(childAlias, childRowKey, result);
                    }

                    if (field.SelectionSet is not null)
                        WalkSelectionSet(childAlias, childIsRealEntity ? childRowKey : rowKey, field.SelectionSet, result);

                    continue;
                }

                if (field.SelectionSet is not null)
                {
                    WalkSelectionSet(alias, rowKey, field.SelectionSet, result);
                    continue;
                }

                if (NodeRegistry.TryResolveLeafField(alias, fieldName, out var resolvedFields, out _))
                {
                    foreach (var (entityAlias, entityColumn) in resolvedFields)
                    {
                        if (NodeRegistry.FrozenEntityTrees.ContainsKey(entityAlias))
                            EnsurePrimaryKey(entityAlias, rowKey, result);

                        result.AddColumn(rowKey, entityAlias, entityColumn);
                    }
                }
            }
        }

        private static void EnsurePrimaryKey(string alias, string rowKey, Result result)
        {
            if (NodeRegistry.FrozenEntityTrees.TryGetValue(alias, out var tree) && tree.UpsertKeys.Count > 0)
            {
                var pkColumn = tree.UpsertKeys[0].Split('~').Last();
                result.AddColumn(rowKey, alias, pkColumn);
            }
            else
            {
                result.AddColumn(rowKey, alias, "Id");
            }
        }
    }
}