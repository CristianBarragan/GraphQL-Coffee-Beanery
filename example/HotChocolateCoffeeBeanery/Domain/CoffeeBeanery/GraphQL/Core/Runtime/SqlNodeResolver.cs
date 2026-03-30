using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public static class SqlNodeResolver
    {
        // ----------------------------------------------------------
        // Mutation parsing
        // ----------------------------------------------------------
        public static void GetMutations(
            Dictionary<string, NodeTree> trees,
            ISyntaxNode node,
            Dictionary<string, SqlNode> linkEntityDictionaryTree,
            Dictionary<string, SqlNode> linkModelDictionaryTree,
            Dictionary<string, SqlNode> sqlStatementNodes,
            NodeTree currentTree,
            string previousNode,
            NodeTree parentTree,
            List<string> models,
            List<string> visitedModels)
        {
            if (node != null && node.GetNodes()?.Count() == 0)
            {
                if (linkModelDictionaryTree.TryGetValue(
                        $"{currentTree.Alias}~{currentTree.Name}~{node.ToString()}",
                        out var sqlNodeFrom))
                {
                    if (linkEntityDictionaryTree.TryGetValue(sqlNodeFrom.RelationshipKey,
                            out var sqlNodeTo))
                    {
                        var value = string.Empty;

                        if (previousNode.Split(':').Length == 2)
                        {
                            if (sqlNodeTo.FromEnumeration.TryGetValue(
                                    previousNode.Split(':')[1].Sanitize().Replace("_", ""),
                                    out var enumValue))
                            {
                                value = sqlNodeTo.FromEnumeration
                                    .FirstOrDefault(e => e.Value.Matches(enumValue)).Value;
                            }
                            else
                            {
                                value = previousNode.Split(':')[1].Sanitize();
                            }
                        }

                        var key = linkModelDictionaryTree
                            .First(a => a.Key.Matches(
                                $"{currentTree.Alias}~{currentTree.Name}~{node.ToString()}")).Key;

                        var fieldName = key.Split('~')[2];

                        // Add the field onto the current entity
                        // e.g. CustomerCustomerRelationship~InnerCustomerKey
                        AddEntity(sqlStatementNodes, sqlNodeTo,
                            $"{sqlNodeTo.Table}~{fieldName}", value);

                        // ── Propagate FK value into the ONE matching child entity ──────
                        // Each FK field (e.g. InnerCustomerKey, OuterCustomerKey) must
                        // map to exactly one aliased child tree.
                        // Strategy: match by alias name contained in field name first
                        // (e.g. "InnerCustomerKey" contains "InnerCustomer")
                        // Fallback: use declaration order index to disambiguate
                        foreach (var childLink in currentTree.Children
                            .Concat(currentTree.RelatedChildren ?? new List<LinkKey>()))
                        {
                            if (!childLink.FromColumn.Matches(fieldName) &&
                                !childLink.FromColumn.Matches(node.ToString()))
                                continue;

                            // Primary: find the ONE tree whose alias is contained in fieldName
                            // e.g. "InnerCustomerKey" contains "InnerCustomer" → InnerCustomer tree
                            //      "OuterCustomerKey" contains "OuterCustomer" → OuterCustomer tree
                            var matchingChildTrees = trees
                                .Where(t =>
                                    t.Value.Name.Matches(childLink.To) &&
                                    fieldName.Contains(t.Key, StringComparison.OrdinalIgnoreCase))
                                .ToList();

                            // Fallback: alias name not in field name — use declaration index
                            // e.g. links declared as [InnerCustomerId, OuterCustomerId]
                            //      trees ordered as  [InnerCustomer, OuterCustomer]
                            //      InnerCustomerId is at index 0 → InnerCustomer (index 0)
                            //      OuterCustomerId is at index 1 → OuterCustomer (index 1)
                            if (!matchingChildTrees.Any())
                            {
                                var allSameEntityTrees = trees
                                    .Where(t => t.Value.Name.Matches(childLink.To))
                                    .OrderBy(t => t.Key)
                                    .ToList();

                                var sameEntityLinks = currentTree.Children
                                    .Concat(currentTree.RelatedChildren ?? new List<LinkKey>())
                                    .Where(l => l.To == childLink.To)
                                    .ToList();

                                var idx = sameEntityLinks
                                    .FindIndex(l => l.FromColumn.Matches(fieldName));

                                if (idx >= 0 && idx < allSameEntityTrees.Count)
                                    matchingChildTrees.Add(allSameEntityTrees[idx]);
                            }

                            foreach (var (childAlias, childTree) in matchingChildTrees)
                            {
                                var childUpsertKey = $"{childAlias}~{childLink.ToColumn}";

                                if (sqlStatementNodes.ContainsKey(childUpsertKey))
                                    continue;

                                if (linkEntityDictionaryTree.TryGetValue(
                                        $"{childAlias}~{childTree.Name}~{childLink.ToColumn}",
                                        out var childEntityNode))
                                {
                                    AddEntity(sqlStatementNodes, childEntityNode,
                                        childUpsertKey, value);
                                }
                                else if (linkModelDictionaryTree.TryGetValue(
                                             $"{childAlias}~{childTree.Name}~{childLink.ToColumn}",
                                             out var childModelNode) &&
                                         linkEntityDictionaryTree.TryGetValue(
                                             childModelNode.RelationshipKey,
                                             out var childEntityNodeViaModel))
                                {
                                    AddEntity(sqlStatementNodes, childEntityNodeViaModel,
                                        childUpsertKey, value);
                                }
                            }
                        }
                    }
                }

                if (!visitedModels.Contains(currentTree.Name))
                    visitedModels.Add(currentTree.Name);

                return;
            }

            if (node == null)
                return;

            foreach (var childNode in node.GetNodes())
            {
                if (models.Any(e => e.Matches(childNode.ToString().Split('{')[0])) ||
                    node.ToString().Matches("nodes") ||
                    node.ToString().Matches("node"))
                {
                    if (node.ToString().Matches("nodes") || node.ToString().Matches("node"))
                        currentTree = trees[models.Last()];
                    else
                        currentTree = trees[childNode.ToString().Split('{')[0]];

                    parentTree = currentTree.Parents.Count == 0
                        ? currentTree
                        : trees[currentTree.Parents[0].To];
                }

                GetMutations(trees, childNode, linkEntityDictionaryTree, linkModelDictionaryTree,
                    sqlStatementNodes, currentTree, node.ToString(),
                    parentTree, models, visitedModels);
            }
        }

        private static void AddEntity(
            Dictionary<string, SqlNode> sqlStatementNodes,
            SqlNode sqlNodeTo,
            string key,
            string value)
        {
            if (sqlStatementNodes.ContainsKey(key))
                return;

            var keyValue = new KeyValuePair<string, SqlNode>(key, sqlNodeTo.Clone() as SqlNode);
            keyValue.Value.Value = value;
            sqlStatementNodes.Add(keyValue.Key, keyValue.Value);
        }

        /// <summary>
        /// Method for getting upsert field information used for the SQL Statement
        /// </summary>
        public static void GetFields(
            Dictionary<string, NodeTree> trees,
            Dictionary<string, NodeTree> entityTrees,
            ISyntaxNode node,
            Dictionary<string, SqlNode> linkEntityDictionaryTree,
            Dictionary<string, SqlNode> linkModelDictionaryTree,
            Dictionary<string, SqlNode> sqlStatementNodes,
            NodeTree currentTree,
            NodeTree parentTree,
            List<string> visitedModels,
            List<string> visitedEntities,
            List<string> models,
            List<string> entities,
            bool isEdge)
        {
            if (node != null && node.GetNodes()?.Count() == 0)
            {
                var currentModel = visitedModels.LastOrDefault();

                var modelTree = trees.FirstOrDefault(a =>
                    a.Value.Mapping.Any(b =>
                        b.DestinationEntity.Matches(currentTree.Name) &&
                        b.DestinationName.Matches(node.ToString())));

                if (linkModelDictionaryTree.TryGetValue(
                        $"{currentTree.Alias}~{currentTree.Name}~{node.ToString()}",
                        out var sqlNodeFrom) ||
                    linkModelDictionaryTree.TryGetValue(
                        $"{currentTree.Alias}~{modelTree.Value?.Alias}~{node.ToString()}",
                        out sqlNodeFrom))
                {
                    if (linkEntityDictionaryTree.TryGetValue(
                            sqlNodeFrom.RelationshipKey, out var sqlNodeTo))
                    {
                        AddField(linkEntityDictionaryTree, sqlStatementNodes,
                            currentTree, sqlNodeTo, isEdge);

                        if (!visitedModels.Contains(currentTree.Alias))
                            visitedModels.Add(currentTree.Alias);
                    }

                    visitedEntities.Add(sqlNodeFrom.Table);

                    AddField(linkEntityDictionaryTree, sqlStatementNodes,
                        currentTree, sqlNodeFrom, isEdge);
                }

                return;
            }

            if (node == null)
                return;

            foreach (var childNode in node.GetNodes())
            {
                if (models.Any(e => e.Matches(childNode.ToString().Split('{')[0])) ||
                    node.ToString().Matches("nodes") ||
                    node.ToString().Matches("node"))
                {
                    if (node.ToString().Matches("nodes") || node.ToString().Matches("node"))
                        currentTree = trees.OrderBy(a => a.Value.Id).First().Value;
                    else
                        currentTree = trees[childNode.ToString().Split('{')[0]];

                    parentTree = currentTree.Parents.Count == 0
                        ? currentTree
                        : trees.First(a =>
                            a.Value.Name.Matches(currentTree.Parents[0].To) &&
                            !visitedModels.Contains(a.Key)).Value;
                }

                GetFields(trees, entityTrees, childNode,
                    linkEntityDictionaryTree, linkModelDictionaryTree,
                    sqlStatementNodes, currentTree, parentTree,
                    visitedModels, visitedEntities, models, entities, isEdge);
            }
        }

        private static void AddField(
            Dictionary<string, SqlNode> linkEntityDictionaryTree,
            Dictionary<string, SqlNode> sqlStatementNodes,
            NodeTree currentTree,
            SqlNode? sqlNode,
            bool isEdge)
        {
            var cloned = sqlNode.Clone() as SqlNode;
            cloned.SqlNodeTypes.Clear();
            cloned.SqlNodeTypes.Add(isEdge ? SqlNodeType.Edge : SqlNodeType.Node);

            if (sqlStatementNodes.ContainsKey(cloned.RelationshipKey))
                sqlStatementNodes[cloned.RelationshipKey] = cloned;

            if (!sqlStatementNodes.ContainsKey(cloned.RelationshipKey))
                sqlStatementNodes.Add(cloned.RelationshipKey, cloned);
        }
    }
}