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
            // objectMap: entityTree.Alias → mapped model instance
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
            // Root is always found in modelTrees — even if IsEntity=false
            var rootModelTree = GetRootFromWrapper<M>(modelTrees);

            // ── Phase 3: recursively build the object graph ───────────────────────
            var visited    = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var rootObject = Build(
                rootModelTree,
                objectMap,
                modelTrees,
                entityTrees,
                visited);

            // ── Phase 4: attach root to Wrapper and add to result ─────────────────
            var wrapperObj = new Wrapper();
            Attach(wrapperObj, rootObject.Object);
            wrappers.Add(wrapperObj);

            return (wrappers.OfType<M>().ToList(),
                sqlStructure.Pagination?.StartCursor,
                sqlStructure.Pagination?.EndCursor,
                totalCount, pageRecords);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // BUILD
        // ─────────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Recursively assembles the model graph starting from modelTree.
        ///
        /// CRITICAL BRANCHING RULE:
        /// - IsEntity = false (model-only, e.g. CustomerCustomerEdge, Product):
        ///     children are declared in ModelToEntityLinks — each link.AliasTo points
        ///     to an entity tree whose mapped model should be attached to this node.
        /// - IsEntity = true (entity-backed, e.g. Customer, Contract):
        ///     children are declared in NodeTree.Children + RelatedChildren — each
        ///     child.AliasTo points to a child entity tree.
        /// </summary>
        private (object Object, string Alias) Build(
            NodeTree modelTree,
            Dictionary<string, object> objectMap,
            Dictionary<string, NodeTree> modelTrees,
            Dictionary<string, NodeTree> entityTrees,
            HashSet<string> visited)
        {
            if (visited.Contains(modelTree.Alias))
                return (objectMap.TryGetValue(modelTree.Alias, out var cached)
                    ? cached
                    : Activator.CreateInstance(modelTree.ModelType)!, string.Empty);

            visited.Add(modelTree.Alias);

            // Get the already-mapped model from Phase 1 or create empty
            if (!objectMap.TryGetValue(modelTree.Alias, out var model))
            {
                model = Activator.CreateInstance(modelTree.ModelType)!;
                objectMap[modelTree.Alias] = model;
            }

            // if (!modelTree.IsEntity)
            // {
                // ── Model-only node: children via ModelToEntityLinks ──────────────
                // Each link.AliasTo = entity tree alias whose model should be
                // attached as a child of this model-only node.
                // Deduplicate by AliasTo to avoid attaching the same child twice.
                var seenLinks = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                foreach (var link in modelTree.ModelToEntityLinks ?? new List<LinkKey>())
                {
                    var childAlias = !string.IsNullOrWhiteSpace(link.AliasTo)
                        ? link.AliasTo
                        : link.To;

                    if (!seenLinks.Add(childAlias)) continue;

                    // Find the child entity tree
                    if (entityTrees.TryGetValue(childAlias, out var childEntityTree))
                    {
                        // Fallback: find by model tree alias
                        childEntityTree = entityTrees.Values
                            .FirstOrDefault(t =>
                                t.Alias.Equals(childAlias,
                                    StringComparison.OrdinalIgnoreCase) ||
                                t.Name.Equals(childAlias,
                                    StringComparison.OrdinalIgnoreCase));

                        if (childEntityTree == null)
                        {
                            Console.ForegroundColor = ConsoleColor.Yellow;
                            Console.WriteLine(
                                $"[WARN] Build(model-only): child entity tree '{childAlias}' " +
                                $"not found. Skipping.");
                            Console.ResetColor();
                            continue;
                        }
                    }

                    // Find matching model tree for this entity tree
                    var childModelTree = modelTrees.Values
                        .FirstOrDefault(t => t.ModelType == childEntityTree.ModelType)
                        ?? modelTrees.Values
                            .FirstOrDefault(t =>
                                t.Name.Equals(childEntityTree.Name,
                                    StringComparison.OrdinalIgnoreCase) ||
                                t.Alias.Equals(childAlias,
                                    StringComparison.OrdinalIgnoreCase));

                    if (childModelTree == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(
                            $"[WARN] Build(model-only): no model tree for '{childAlias}'. " +
                            $"Skipping.");
                        Console.ResetColor();
                        continue;
                    }

                    // Recursively build child — it may be IsEntity=true, so it will
                    // use its own Children links from there
                    var childObject = Build(
                        childModelTree,
                        objectMap,
                        modelTrees,
                        entityTrees,
                        visited);

                    if (childObject.Alias.Matches(link.AliasTo))
                    {
                        Attach(model, childObject.Object);    
                    };
                }
            // }
            // else
            // {
                // ── Entity-backed node: children via NodeTree.Children + RelatedChildren ──
                // Find the corresponding entity tree (same alias or ModelType match)
                var entityTree = entityTrees.TryGetValue(modelTree.Alias, out var et)
                    ? et
                    : entityTrees.Values.FirstOrDefault(t =>
                        t.ModelType == modelTree.ModelType && t.IsEntity);

                if (entityTree == null)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine(
                        $"[WARN] Build(entity): no entity tree for model '{modelTree.Alias}'. " +
                        $"Returning model as-is.");
                    Console.ResetColor();
                    return (model, string.Empty);
                }

                var seenChildren = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                var allChildLinks = entityTree.Children
                    .Concat(entityTree.RelatedChildren)
                    .ToList();

                foreach (var childLink in allChildLinks)
                {
                    var childAlias = !string.IsNullOrWhiteSpace(childLink.AliasTo)
                        ? childLink.AliasTo
                        : childLink.To;

                    if (!seenChildren.Add(childAlias)) continue;

                    if (!entityTrees.TryGetValue(childAlias, out var childEntityTree))
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(
                            $"[WARN] Build(entity): child entity tree '{childAlias}' " +
                            $"not found. Skipping.");
                        Console.ResetColor();
                        continue;
                    }

                    var childModelTree = modelTrees.Values
                        .FirstOrDefault(t => t.ModelType == childEntityTree.ModelType)
                        ?? modelTrees.Values
                            .FirstOrDefault(t =>
                                t.Name.Equals(childEntityTree.Name,
                                    StringComparison.OrdinalIgnoreCase));

                    if (childModelTree == null)
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine(
                            $"[WARN] Build(entity): no model tree for '{childAlias}'. Skipping.");
                        Console.ResetColor();
                        continue;
                    }

                    var childObject = Build(
                        childModelTree,
                        objectMap,
                        modelTrees,
                        entityTrees,
                        visited);

                    if (childObject.Alias.Matches(childLink.AliasTo))
                    {
                        Attach(model, childObject.Object);    
                    }
                }
            // }

            return (model, modelTree.Alias);
        }

        // ─────────────────────────────────────────────────────────────────────────
        // ATTACH
        // ─────────────────────────────────────────────────────────────────────────

        private static void Attach(object parent, object child)
        {
            if (parent == null || child == null) return;

            var prop = ResolveNavigationProperty(parent.GetType(), child.GetType());
            
            if (prop == null) return;
            
            if (typeof(IList).IsAssignableFrom(prop.PropertyType))
            {
                var list = prop.GetValue(parent) as IList;
                if (list == null)
                {
                    list = (IList)Activator.CreateInstance(prop.PropertyType)!;
                    prop.SetValue(parent, list);
                }

                if (!list.Contains(child))
                    list.Add(child);
            }
            else
            {
                // Only set scalar if unfilled — preserves InnerCustomer slot
                // when OuterCustomer arrives
                if (prop.GetValue(parent) == null)
                    prop.SetValue(parent, child);
            }
        }

        // ─────────────────────────────────────────────────────────────────────────
        // HELPERS
        // ─────────────────────────────────────────────────────────────────────────

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

        private static PropertyInfo? ResolveNavigationProperty(Type parentType, Type childType)
        {
            return parentType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p =>
                {
                    if (p.PropertyType == childType) return true;
                    if (!p.PropertyType.IsGenericType) return false;
                    return p.PropertyType.GetGenericTypeDefinition() == typeof(List<>)
                        && p.PropertyType.GetGenericArguments()[0] == childType;
                });
        }

        private static NodeTree? ResolveEntityTree(
            string? alias,
            Type entityType,
            Type? modelType,
            int seen,
            Dictionary<string, NodeTree> entityTrees)
        {
            if (alias != null && entityTrees.TryGetValue(alias, out var byAlias))
                return byAlias;

            var candidates = entityTrees.Values
                .Where(t => t.EntityType == entityType &&
                            (modelType == null || t.ModelType == modelType))
                .OrderBy(t => t.Id)
                .ToList();

            if (!candidates.Any())
                candidates = entityTrees.Values
                    .Where(t => t.EntityType?.Name.Equals(entityType.Name,
                        StringComparison.OrdinalIgnoreCase) == true)
                    .OrderBy(t => t.Id)
                    .ToList();

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
}