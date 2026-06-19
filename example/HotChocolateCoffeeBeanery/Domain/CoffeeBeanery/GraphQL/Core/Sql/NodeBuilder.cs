using System.Reflection;
using Microsoft.EntityFrameworkCore;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Helper;

namespace CoffeeBeanery.GraphQL.Core.Sql;

public sealed class NodeBuilder<TContext>
    where TContext : DbContext
{
    private readonly EfEntityMetadata<TContext> _metadata;

    public NodeBuilder(EfEntityMetadata<TContext> metadata)
    {
        _metadata = metadata;
    }

    public void BuildFromMappings()
    {
        var all = MappingRegistry.GetAll();

        foreach (var (_, map) in all)
            InferModelChildren(map);

        foreach (var (_, map) in all)
            GenerateReflectedFieldMaps(map);

        foreach (var (_, map) in all)
            ResolveFieldMapAliases(map);

        // BuildTree populates NodeRegistry.ModelTrees / EntityTrees for every alias - including
        // role-scoped ones (InnerCustomerCustomer, OuterCustomerCustomer, etc.) that don't exist
        // as their own top-level entries in `all`. BuildFieldIndex (below) depends on that being
        // fully populated first, since it now indexes off the registries, not off `all`.
        foreach (var (alias, map) in all)
            BuildTree(alias, map);

        foreach (var (alias, map) in all)
            BuildModel(alias, map);

        // FIX: previously iterated `all` (canonical maps only), so ColumnByField/EnumByField/
        // ChildAliasByField only ever got registered under each map's own canonical alias
        // (e.g. "Customer"). Any role-scoped alias (e.g. "InnerCustomerCustomer") had no field
        // index at all, so MutationGraphWalker had nowhere to land once it descended into a
        // role-scoped child. Now indexes every alias actually registered by BuildTree.
        BuildFieldIndexForAllRegisteredTrees();

        NodeRegistry.Freeze();
    }

    private readonly HashSet<Type> ScalarTypes = new()
    {
        typeof(string), typeof(Guid), typeof(DateTime), typeof(DateTimeOffset),
        typeof(decimal), typeof(bool),
        typeof(int), typeof(long), typeof(double), typeof(float)
    };

    private void InferModelChildren(NodeMap map)
    {
        if (map.ModelType == null) return;

        var existingFieldNames = map.ModelChildren
            .Select(x => x.FieldName)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var p in map.ModelType.GetProperties())
        {
            var type = UnwrapCollection(p.PropertyType);
            if (IsScalar(type)) continue;

            var fieldName = ToGraphQlFieldName(p.Name);

            if (!existingFieldNames.Contains(fieldName))
            {
                map.ModelChildren.Add(new ModelKey
                {
                    To = type.Name,
                    FieldName = fieldName
                });
                existingFieldNames.Add(fieldName);
            }
        }

        if (map.EntityType != null)
        {
            var efEntityType = _metadata.RequireEntityType(map.EntityType, map.ModelName);

            foreach (var nav in _metadata.GetNavigations(efEntityType))
            {
                var fieldName = ToGraphQlFieldName(nav.NavigationName);

                if (existingFieldNames.Contains(fieldName)) continue;

                map.ModelChildren.Add(new ModelKey
                {
                    To = nav.RelatedEntityType.Name,
                    FieldName = fieldName
                });
                existingFieldNames.Add(fieldName);
            }
        }
    }

    // ------------------------------------------------------------------
    // Field index - driven off the fully-populated registries rather than
    // the canonical `all` map list, so role-scoped aliases get their own
    // ColumnByField/EnumByField entries too, not just each map's own
    // canonical alias.
    // ------------------------------------------------------------------

    private void BuildFieldIndexForAllRegisteredTrees()
    {
        var processedAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var (alias, tree) in NodeRegistry.EntityTrees)
        {
            if (!processedAliases.Add(alias)) continue;
            BuildFieldIndex(alias, tree.NodeMap);
        }

        foreach (var (alias, tree) in NodeRegistry.ModelTrees)
        {
            if (!processedAliases.Add(alias)) continue;
            BuildFieldIndex(alias, tree.NodeMap);
        }
    }

    private void BuildFieldIndex(string alias, NodeMap map)
    {
        foreach (var field in map.FieldMaps)
        {
            var graphQlFieldName = ToGraphQlFieldName(field.SourceName);
            
            var targetsOwnEntity = map.EntityType is not null &&
                string.Equals(field.DestinationEntity, map.EntityType.Name, StringComparison.OrdinalIgnoreCase);

            var entityAlias = targetsOwnEntity
                ? alias
                : (string.IsNullOrEmpty(field.DestinationAlias) ? alias : field.DestinationAlias);

            var key = (alias, graphQlFieldName);

            if (!NodeRegistry.ColumnByField.TryGetValue(key, out var fields))
            {
                fields = new List<(string EntityAlias, string EntityColumn)>();
                NodeRegistry.ColumnByField[key] = fields;
            }

            if (!fields.Any(f =>
                    string.Equals(f.EntityAlias, entityAlias, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(f.EntityColumn, field.DestinationName, StringComparison.OrdinalIgnoreCase)))
            {
                fields.Add((entityAlias, field.DestinationName));
            }

            if (field.ToEnum is { Count: > 0 })
                NodeRegistry.EnumByField[key] = field.ToEnum;
        }
    }

    /// <summary>
    /// Resolves a child map for ENTITY-side recursion only. Must never return a model-only
    /// aggregate (IsModel=true, IsEntity=false, e.g. Product) - that map has no single
    /// EntityType of its own, and EnsureRoleScopedTree would blow up (or silently mis-register)
    /// trying to read EntityType off it. Only matches maps that are themselves entities.
    /// </summary>
    private NodeMap? ResolveEntityChildMap(string targetEntityTypeName)
    {
        return MappingRegistry.GetAll().Values
            .FirstOrDefault(m =>
                m.IsEntity &&
                string.Equals(m.EntityType?.Name, targetEntityTypeName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// Resolves a child map for MODEL-side recursion only. Must never fall through to an
    /// entity-only map - that would pull in something with no ModelType, which is meaningless
    /// in a ModelTrees context. Only matches maps that are themselves models.
    /// </summary>
    private NodeMap? ResolveModelChildMap(string fieldName, string targetModelTypeName)
    {
        var childMap = MappingRegistry.GetAll().Values
            .FirstOrDefault(m =>
                m.IsModel &&
                string.Equals(m.ModelName, fieldName, StringComparison.OrdinalIgnoreCase));

        childMap ??= MappingRegistry.GetAll().Values
            .FirstOrDefault(m =>
                m.IsModel &&
                string.Equals(m.ModelType?.Name, targetModelTypeName, StringComparison.OrdinalIgnoreCase));

        return childMap;
    }

    private static void WarnUnresolvedChild(string alias, string fieldName, string targetTypeName, string context)
    {
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(
            $"[WARNING] No NodeMap registered for {context} target '{targetTypeName}' " +
            $"(field '{fieldName}' on alias '{alias}') - this field will never " +
            $"resolve in MutationGraphWalker/QueryGraphWalker.");
        Console.ResetColor();
    }

    private static string ToGraphQlFieldName(string clrName)
    {
        if (string.IsNullOrEmpty(clrName)) return clrName;
        if (clrName.Length == 1) return clrName.ToLowerInvariant();
        return char.ToLowerInvariant(clrName[0]) + clrName.Substring(1);
    }

    private bool IsScalar(Type t)
    {
        var u = Nullable.GetUnderlyingType(t) ?? t;
        return u.IsEnum || ScalarTypes.Contains(u);
    }

    private Type UnwrapCollection(Type t)
    {
        if (t == typeof(string)) return t;

        if (t.IsGenericType)
        {
            var def = t.GetGenericTypeDefinition();
            if (def == typeof(List<>) || def == typeof(IEnumerable<>))
                return t.GetGenericArguments()[0];
        }

        return t;
    }

    private void GenerateReflectedFieldMaps(NodeMap map)
    {
        if (map.ModelType == null || map.ModelToEntity.Count == 0)
            return;

        var props = map.ModelType.GetProperties();

        var entities = map.ModelToEntity
            .Select(x => x.EntityType)
            .Distinct()
            .ToList();

        foreach (var et in map.ModelToEntity)
        {
            foreach (var field in map.FieldMaps.Where(a => a.SourceModel.Matches(et.From)))
            {
                field.SourceAlias = et.AliasFrom;
                field.DestinationAlias = et.AliasTo;
            }
        }

        foreach (var mp in props)
        {
            if (!IsScalar(mp.PropertyType)) continue;

            foreach (var et in entities)
            {
                var ep = et.GetProperty(mp.Name);
                if (ep == null) continue;

                map.FieldMaps.Add(new FieldMap
                {
                    DestinationAlias = $"{map.Prefix}{mp.Name}",
                    SourceAlias = map.Alias,
                    SourceModel = map.ModelName,
                    SourceName = mp.Name,
                    DestinationEntity = et.Name,
                    DestinationName = ep.Name
                });
            }
        }
    }

    private void ResolveFieldMapAliases(NodeMap map)
    {
        foreach (var f in map.FieldMaps)
        {
            f.SourceAlias = map.Alias;

            if (map.EntityType is not null &&
                string.Equals(map.EntityType.Name, f.DestinationEntity, StringComparison.OrdinalIgnoreCase))
            {
                f.DestinationAlias = $"{map.Prefix}{f.DestinationEntity}";
                continue;
            }

            var targetMap = MappingRegistry.GetAll().Values
                .FirstOrDefault(m =>
                    string.Equals(m.EntityType?.Name, f.DestinationEntity, StringComparison.OrdinalIgnoreCase));

            f.DestinationAlias = targetMap?.Alias ?? map.Alias;
        }
    }

    // ------------------------------------------------------------------
    // Tree building
    // ------------------------------------------------------------------

    public void BuildTree(string alias, NodeMap map)
    {
        var modelToEntityKeys = BuildModelToEntityKeys(map);

        // Independent recursion state per side, per call - a model recursion and an entity
        // recursion happening for the same root map must never share visited-sets, or one
        // side's cycle-guard could suppress the other's legitimate traversal.
        var visitedModelAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { alias };
        var activePathModelTypes = new HashSet<Type>();

        var visitedEntityAliases = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { alias };
        var activePathEntityTypes = new HashSet<Type>();

        if (map.IsModel)
        {
            var modelChildren = BuildModelChildren(map, alias, map.Prefix, visitedModelAliases, activePathModelTypes);

            NodeRegistry.ModelTrees[alias] = new ModelNodeTree
            {
                Alias = alias,
                ModelName = map.ModelName,
                Name = map.ModelType.Name,
                NodeMap = map,
                ModelChildren = modelChildren,
                ModelToEntity = modelToEntityKeys,
                Mapping = map.FieldMaps,
                Schema = map.Schema,
                ModelType = map.ModelType,
                EntityType = map.EntityType,
                IsModel = map.IsModel,
                IsEntity = map.IsEntity,
                Prefix = map.Prefix
            };
        }

        if (map.IsEntity)
        {
            var (children, related) =
                BuildEntityChildren(map, alias, map.Prefix, visitedEntityAliases, activePathEntityTypes);

            NodeRegistry.EntityTrees[alias] = new EntityNodeTree
            {
                Alias = alias,
                ModelName = map.ModelName,
                Name = map.EntityType!.Name,
                NodeMap = map,
                EntityChildren = children,
                EntityChildrenRelated = related,
                ModelToEntity = modelToEntityKeys,
                Mapping = map.FieldMaps,
                UpsertKeys = map.UpsertKeys.Select(k => k.Key).ToList(),
                IsGraph = map.IsGraph,
                GraphMap = map.GraphMap,
                Schema = map.Schema,
                EntityType = map.EntityType,
                ModelType = map.ModelType,
                IsModel = map.IsModel,
                IsEntity = map.IsEntity,
                Prefix = map.Prefix
            };
        }

        // ModelToEntity links (CustomerCustomerEdge -> Customer via InnerCustomer/OuterCustomer)
        // are NOT discoverable by walking EF navigations - the EF nav direction lives on
        // CustomerCustomerRelationship (InnerCustomer/OuterCustomer nav props pointing back to
        // Customer), a different entity than this model's own EntityType, and the model graph
        // can diverge from that structure at any time. This is the only place that knows the
        // role (AliasProperty), so it's the only place that can register the role-scoped
        // EntityTree (e.g. "InnerCustomerCustomer") and the field that reaches it.
        RegisterModelToEntityChildren(map, alias, modelToEntityKeys, map.Prefix, visitedEntityAliases, activePathEntityTypes);
    }

    private static string ModelToEntityAlias(NodeMap map, EntityKey k)
    {
        // An explicit role (AliasProperty, declared via AddModelToEntity's `alias:` param)
        // always wins - it's the one piece of data that distinguishes InnerCustomer from
        // OuterCustomer, and must produce "InnerCustomerCustomer"/"OuterCustomerCustomer" to
        // match what SqlHelper expects.
        if (!string.IsNullOrEmpty(k.AliasProperty))
            return $"{k.AliasProperty}{k.EntityType.Name}";

        var occurrencesOfType = map.ModelToEntity.Count(x => x.EntityType == k.EntityType);

        return occurrencesOfType > 1
            ? $"{map.Alias}{k.FromColumn}"
            : $"{map.Alias}{k.EntityType.Name}";
    }

    private List<EntityKey> BuildModelToEntityKeys(NodeMap map)
    {
        var list = new List<EntityKey>();

        foreach (var k in map.ModelToEntity)
        {
            var targetMap = MappingRegistry.GetAll().Values
                .FirstOrDefault(m => m.EntityType == k.EntityType);

            if (targetMap == null)
            {
                WarnUnresolvedChild(map.Alias, k.AliasProperty, k.EntityType?.Name ?? "(unknown)", "ModelToEntity link");
                continue;
            }

            list.Add(new EntityKey
            {
                From = map.ModelType.Name,
                AliasFrom = map.Alias,
                To = k.EntityType.Name,
                AliasTo = ModelToEntityAlias(map, k),
                FromColumn = k.FromColumn,
                ToColumn = k.ToColumn,
                EntityType = k.EntityType,
                AliasProperty = k.AliasProperty
            });
        }

        return list;
    }

    /// <summary>
    /// Registers a role-scoped EntityTree for every ModelToEntity link that declares an
    /// explicit role (AliasProperty), and wires up the GraphQL field (e.g. "innerCustomer")
    /// that reaches it - independent of, and in addition to, whatever EF navigations
    /// BuildEntityChildren finds on the model's own EntityType.
    /// </summary>
    private void RegisterModelToEntityChildren(
        NodeMap map,
        string alias,
        List<EntityKey> modelToEntityKeys,
        string prefix,
        HashSet<string> visitedAliases,
        HashSet<Type> activePathEntityTypes)
    {
        foreach (var k in modelToEntityKeys)
        {
            if (string.IsNullOrEmpty(k.AliasProperty))
                continue; // unaliased links (e.g. flat multi-entity aggregates like Product) have
                          // no nested GraphQL object of their own - their fields resolve directly
                          // via FieldMaps/ColumnByField, not via a child alias.

            var canonicalChildMap = MappingRegistry.GetAll().Values
                .FirstOrDefault(m => m.IsEntity && m.EntityType == k.EntityType);

            if (canonicalChildMap is null)
                continue; // already warned in BuildModelToEntityKeys

            var fieldName = ToGraphQlFieldName(k.AliasProperty);

            NodeRegistry.ChildAliasByField[(alias, fieldName)] = k.AliasTo;

            EnsureRoleScopedTree(k.AliasTo, prefix, canonicalChildMap, visitedAliases, activePathEntityTypes);
        }
    }

    private (List<EntityKey>, List<EntityKey>) BuildEntityChildren(
        NodeMap map,
        string alias,
        string prefix,
        HashSet<string> visitedAliases,
        HashSet<Type> activePathEntityTypes)
    {
        var children = new List<EntityKey>();
        var related = new List<EntityKey>();

        if (map.EntityType == null) return (children, related);

        if (!activePathEntityTypes.Add(map.EntityType))
            return (children, related);

        try
        {
            var efEntityType = _metadata.RequireEntityType(map.EntityType, map.ModelName);
            var navs = _metadata.GetNavigations(efEntityType).ToList();

            // Only navigations that target the SAME related type more than once from THIS
            // entity are genuinely ambiguous (e.g. CustomerCustomerRelationship -> Customer via
            // both InnerCustomer and OuterCustomer). Everything else converges on its canonical
            // alias regardless of which path reached it - this is what bounds the tree, instead
            // of every distinct path through the schema minting a brand new alias and rebuilding
            // a fresh subtree (the cause of the 26 -> 282 explosion).
            var navCountByRelatedType = navs
                .GroupBy(n => n.RelatedEntityType)
                .ToDictionary(g => g.Key, g => g.Count());

            foreach (var nav in navs)
            {
                var canonicalChildMap = ResolveEntityChildMap(nav.RelatedEntityType.Name);

                if (canonicalChildMap is null)
                {
                    WarnUnresolvedChild(alias, nav.NavigationName, nav.RelatedEntityType.Name, "entity navigation");
                    continue;
                }

                var isAmbiguous = navCountByRelatedType[nav.RelatedEntityType] > 1;

                var childAlias = isAmbiguous
                    ? $"{alias}{nav.NavigationName}"
                    : canonicalChildMap.Alias;

                var (fromColumn, toColumn) = nav.IsOnDependent
                    ? (nav.PrincipalKeyProperty, nav.ForeignKeyProperty)
                    : (nav.ForeignKeyProperty, nav.PrincipalKeyProperty);

                var key = new EntityKey
                {
                    From = map.EntityType.Name,
                    AliasFrom = alias,
                    To = nav.RelatedEntityType.Name,
                    AliasTo = childAlias,
                    AliasProperty = nav.NavigationName,
                    FromColumn = fromColumn,
                    ToColumn = toColumn,
                    EntityType = nav.RelatedEntityType
                };

                if (nav.IsOnDependent)
                    related.Add(key);
                else if (nav.IsCollection)
                    children.Add(key);
                else
                    related.Add(key);

                NodeRegistry.ChildAliasByField[(alias, ToGraphQlFieldName(nav.NavigationName))] = childAlias;

                EnsureRoleScopedTree(childAlias, prefix, canonicalChildMap, visitedAliases, activePathEntityTypes);
            }

            return (children, related);
        }
        finally
        {
            activePathEntityTypes.Remove(map.EntityType);
        }
    }

    private void EnsureRoleScopedTree(
        string alias,
        string prefix,
        NodeMap canonical,
        HashSet<string> visitedAliases,
        HashSet<Type> activePathEntityTypes)
    {
        if (NodeRegistry.EntityTrees.ContainsKey(alias))
            return;

        if (!visitedAliases.Add(alias))
            return;

        var (grandchildren, grandchildrenRelated) =
            BuildEntityChildren(canonical, alias, prefix, visitedAliases, activePathEntityTypes);

        NodeRegistry.EntityTrees[alias] = new EntityNodeTree
        {
            Alias = alias,
            ModelName = canonical.ModelName,
            Name = canonical.EntityType!.Name,
            NodeMap = canonical,
            EntityChildren = grandchildren,
            EntityChildrenRelated = grandchildrenRelated,
            ModelToEntity = new List<EntityKey>(),
            Mapping = canonical.FieldMaps,
            UpsertKeys = canonical.UpsertKeys.Select(k => k.Key).ToList(),
            IsGraph = canonical.IsGraph,
            GraphMap = canonical.GraphMap,
            Schema = canonical.Schema,
            EntityType = canonical.EntityType,
            ModelType = canonical.ModelType,
            IsModel = canonical.IsModel,
            IsEntity = canonical.IsEntity,
            Prefix = prefix
        };
    }

    private List<ModelKey> BuildModelChildren(
        NodeMap map,
        string alias,
        string prefix,
        HashSet<string> visitedAliases,
        HashSet<Type> activePathModelTypes)
    {
        var children = new List<ModelKey>();

        if (map.ModelType == null) return children;

        if (!activePathModelTypes.Add(map.ModelType))
            return children;

        try
        {
            foreach (var child in map.ModelChildren)
            {
                var fieldName = string.IsNullOrEmpty(child.FieldName)
                    ? ToGraphQlFieldName(child.To)
                    : child.FieldName;

                var canonicalChildMap = ResolveModelChildMap(fieldName, child.To);

                if (canonicalChildMap is null)
                {
                    WarnUnresolvedChild(alias, fieldName, child.To, "model navigation");
                    continue;
                }

                var childAlias = $"{alias}{fieldName}";

                children.Add(new ModelKey
                {
                    To = child.To,
                    FieldName = fieldName,
                    AliasTo = childAlias
                });

                NodeRegistry.ChildAliasByField[(alias, fieldName)] = childAlias;

                EnsureRoleScopedModelTree(childAlias, prefix, canonicalChildMap, visitedAliases, activePathModelTypes);
            }

            return children;
        }
        finally
        {
            activePathModelTypes.Remove(map.ModelType);
        }
    }

    private void EnsureRoleScopedModelTree(
        string alias,
        string prefix,
        NodeMap canonical,
        HashSet<string> visitedAliases,
        HashSet<Type> activePathModelTypes)
    {
        if (NodeRegistry.ModelTrees.ContainsKey(alias))
            return;

        if (!visitedAliases.Add(alias))
            return;

        var grandchildren = BuildModelChildren(canonical, alias, prefix, visitedAliases, activePathModelTypes);

        NodeRegistry.ModelTrees[alias] = new ModelNodeTree
        {
            Alias = alias,
            ModelName = canonical.ModelName,
            Name = canonical.ModelType.Name,
            NodeMap = canonical,
            ModelChildren = grandchildren,
            ModelToEntity = new List<EntityKey>(),
            Mapping = canonical.FieldMaps,
            Schema = canonical.Schema,
            ModelType = canonical.ModelType,
            EntityType = canonical.EntityType,
            IsModel = canonical.IsModel,
            IsEntity = canonical.IsEntity,
            Prefix = prefix
        };
    }

    public void BuildModel(string model, NodeMap map)
    {
        var alias = map.Alias;

        foreach (var field in map.FieldMaps)
        {
            var key = $"{alias}~{map.ModelType.Name}~{field.SourceName}";
            var entityKey = key;

            var modelNode = new ModelNode
            {
                RelationshipKey = entityKey,
                Column = field.DestinationName,
                SourceColumn = field.SourceName
            };

            var entityNode = new EntityNode
            {
                RelationshipKey = entityKey,
                EntityKey = entityKey,
                Column = field.DestinationName,
                SourceColumn = field.SourceName,
                Alias = alias,
                Table = map.EntityType?.Name ?? ""
            };

            NodeRegistry.RegisterNode(key, entityKey, modelNode, map.ModelType, map.EntityType!, map.IsEntity);
            NodeRegistry.RegisterNode(key, entityKey, entityNode, map.ModelType, map.EntityType!, map.IsEntity);
        }
    }
}