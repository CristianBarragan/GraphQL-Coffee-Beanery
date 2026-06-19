using System.Collections.Frozen;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.Service.Materialization
{
    public static class DynamicGraphMaterializer
    {
        public static List<M> Materialize<M>(
            string rootAlias,                     // alias the caller walked the query/mutation from - same
                                                    // entityTree.Alias ProcessService already has on hand
            IReadOnlyList<string> aliasOrder,
            IReadOnlyList<object?[]> rowMatrix)
            where M : class
        {
            if (aliasOrder.Count == 0 || rowMatrix.Count == 0)
                return new List<M>();

            if (!aliasOrder.Contains(rootAlias, StringComparer.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"rootAlias '{rootAlias}' is not present in aliasOrder [{string.Join(", ", aliasOrder)}]. " +
                    "The root alias must always be requested/selected (QueryGraphWalker seeds its PK automatically).");

            NodeRegistry.Freeze();

            var modelTrees = NodeRegistry.FrozenModelTrees;
            var entityTrees = NodeRegistry.FrozenEntityTrees;
            var factories = NodeRegistry.FrozenModelFactories;
            var appliers = NodeRegistry.FrozenEntityToModelAppliers;
            var keyGetters = NodeRegistry.FrozenKeyGetters;
            var attachers = NodeRegistry.FrozenAttachers;

            var trees = aliasOrder.ToDictionary(a => a, a => ResolveTree(a, modelTrees, entityTrees), StringComparer.OrdinalIgnoreCase);

            var modelCache = new Dictionary<string, Dictionary<string, object>>(StringComparer.OrdinalIgnoreCase);
            var modelOrder = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            var attached = new Dictionary<(object Parent, string ChildAlias), HashSet<string>>();

            foreach (var alias in aliasOrder)
            {
                modelCache[alias] = new Dictionary<string, object>(StringComparer.Ordinal);
                modelOrder[alias] = new List<string>();
            }

            foreach (var row in rowMatrix)
            {
                var rowModels = new Dictionary<string, (object Model, string Key)>(StringComparer.OrdinalIgnoreCase);

                for (var i = 0; i < aliasOrder.Count && i < row.Length; i++)
                {
                    var alias = aliasOrder[i];
                    var entityInstance = row[i];
                    if (entityInstance is null) continue;

                    if (!keyGetters.TryGetValue(alias, out var keyGetter))
                        continue;

                    var pk = keyGetter(entityInstance);
                    if (pk is null) continue;

                    if (!modelCache[alias].TryGetValue(pk, out var modelInstance))
                    {
                        modelInstance = CreateModelFromEntity(alias, entityInstance, factories, appliers);
                        modelCache[alias][pk] = modelInstance;
                        modelOrder[alias].Add(pk);
                    }

                    rowModels[alias] = (modelInstance, pk);
                }

                foreach (var alias in aliasOrder)
                {
                    if (!rowModels.TryGetValue(alias, out var parent)) continue;
                    var tree = trees[alias];

                    foreach (var childKey in tree.Children)
                    {
                        var childAlias = aliasOrder.FirstOrDefault(a =>
                            string.Equals(a, childKey.AliasTo, StringComparison.OrdinalIgnoreCase));

                        if (childAlias is null || !rowModels.TryGetValue(childAlias, out var child))
                            continue;

                        if (!attachers.TryGetValue((alias, childAlias), out var attach))
                            continue;

                        var trackKey = (parent.Model, childAlias);
                        if (!attached.TryGetValue(trackKey, out var seen))
                            attached[trackKey] = seen = new HashSet<string>(StringComparer.Ordinal);

                        if (seen.Add(child.Key))
                            attach(parent.Model, child.Model);
                    }
                }
            }

            return modelOrder[rootAlias].Select(k => (M)modelCache[rootAlias][k]).ToList();
        }

        private sealed record Tree(
            string Alias,
            Type ModelType,
            Type? EntityType,
            List<EntityKey> Children);        // EntityChildren + EntityChildrenRelated

        private static Tree ResolveTree(
            string alias,
            FrozenDictionary<string, ModelNodeTree> modelTrees,
            FrozenDictionary<string, EntityNodeTree> entityTrees)
        {
            if (entityTrees.TryGetValue(alias, out var et))
            {
                return new Tree(
                    alias,
                    et.ModelType,
                    et.EntityType,
                    et.EntityChildren.Concat(et.EntityChildrenRelated).ToList());
            }

            if (modelTrees.TryGetValue(alias, out var mt))
            {
                return new Tree(
                    alias,
                    mt.ModelType,
                    mt.EntityType,
                    new List<EntityKey>()); // model-only nodes (Product, Wrapper) carry no entity join graph
            }

            throw new InvalidOperationException(
                $"No EntityNodeTree or ModelNodeTree registered for alias '{alias}'. " +
                "Make sure its MappingSet.Register(...) ran before this query executed.");
        }

        private static object CreateModelFromEntity(
            string alias,
            object entityInstance,
            FrozenDictionary<string, Func<object>> factories,
            FrozenDictionary<string, Action<object, object>> appliers)
        {
            if (!factories.TryGetValue(alias, out var factory))
                throw new InvalidOperationException(
                    $"No compiled model factory registered for alias '{alias}'. " +
                    "Make sure its mapping class's generated Register() ran (NodeTreeEmitter.EmitModelFactory).");

            var model = factory();

            if (appliers.TryGetValue(alias, out var apply))
                apply(entityInstance, model);

            return model;
        }
    }
}