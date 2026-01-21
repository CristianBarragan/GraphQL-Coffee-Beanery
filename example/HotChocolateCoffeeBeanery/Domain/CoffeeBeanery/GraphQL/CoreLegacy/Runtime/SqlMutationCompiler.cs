using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.Extension;
using CoffeeBeanery.GraphQL.Core.Helper;
using CoffeeBeanery.GraphQL.Core.Mapping;
using HotChocolate.Language;
using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlMutationCompiler
    {
        public static void CompileMutations<D, S>(
            SqlCompilationContext ctx,
            ISelection graphSelection,
            IEntityTreeMap<D, S> entityTreeMap,
            IModelTreeMap<D, S> modelTreeMap,
            string rootModelName,
            string rootEntityName,
            string wrapperEntityName,
            List<string> models)
            where D : class
            where S : class
        {
            var syntax = graphSelection.SyntaxNode;

            var mutationBody = syntax.GetNodes()
                .FirstOrDefault(n => n.GetNodes().Any());

            if (mutationBody == null)
                return;

            var sqlNodes = new Dictionary<string, SqlNode>();
            var visited = new List<string>();
            var generatedQuery = new Dictionary<string, string>();

            var children = mutationBody.GetNodes().ToList();
            if (children.Count > 1 &&
                children[1].ToString().TrimStart().StartsWith("["))
            {
                foreach (var item in children[1].GetNodes())
                {
                    CollectMutation<D, S>(
                        item,
                        sqlNodes,
                        entityTreeMap,
                        rootModelName,
                        models,
                        visited);
                }
            }
            else
            {
                CollectMutation<D, S>(
                    mutationBody,
                    sqlNodes,
                    entityTreeMap,
                    rootModelName,
                    models,
                    visited);
            }

            SqlHelper.GenerateUpsertStatements(
                entityTreeMap.DictionaryTree,
                sqlNodes,
                rootEntityName,
                wrapperEntityName,
                generatedQuery,
                entityTreeMap.LinkDictionaryTreeMutation,
                entityTreeMap.DictionaryTree[rootEntityName],
                entityTreeMap.EntityNames,
                ctx.Where,
                new List<string>());

            if (generatedQuery.Count > 0)
            {
                ctx.UpsertSql = @"LOAD 'age';
                                   SET search_path = ag_catalog, ""$user"", public; "
                                  + string.Join(";", generatedQuery.Values.Order());
            }
        }

        private static void CollectMutation<D, S>(
            ISyntaxNode node,
            Dictionary<string, SqlNode> sqlNodes,
            IEntityTreeMap<D, S> entityTreeMap,
            string currentModel,
            List<string> models,
            List<string> visited)
            where D : class
            where S : class
        {
            if (node.GetNodes()?.Count() == 0)
            {
                var text = node.ToString();
                var beforeColon = text.Split(':')[0];

                if (!models.Contains(currentModel) &&
                    entityTreeMap.LinkDictionaryTreeMutation.TryGetValue(
                        $"{currentModel}~{beforeColon}", out var fromNode))
                {
                    var linkKey = fromNode.LinkKeys.FirstOrDefault()?.To;
                    if (linkKey != null &&
                        entityTreeMap.LinkDictionaryTreeMutation.TryGetValue(linkKey, out var toNode))
                    {
                        toNode.SqlNodeType = SqlNodeType.Mutation;
                        toNode.Value = text.Contains(":")
                            ? text.Split(':')[1].Trim().Trim('"')
                            : "";

                        AddMutationEntity(entityTreeMap.LinkDictionaryTreeMutation, sqlNodes, toNode);

                        if (!visited.Contains(currentModel))
                            visited.Add(currentModel);
                    }
                }
                return;
            }

            foreach (var child in node.GetNodes())
            {
                var name = child.ToString().Split('{')[0].Trim();
                var nextModel = models.Contains(name) ? name : currentModel;
                CollectMutation<D, S>(
                    child,
                    sqlNodes,
                    entityTreeMap,
                    nextModel,
                    models,
                    visited);
            }
        }

        private static void AddMutationEntity(
            Dictionary<string, SqlNode> linkMutation,
            Dictionary<string, SqlNode> statementNodes,
            SqlNode sqlNode)
        {
            foreach (var kv in linkMutation
                .Where(v => v.Key.Split('~')[1].Matches(sqlNode.Column)))
            {
                kv.Value.Value = sqlNode.Value;
                kv.Value.SqlNodeType = SqlNodeType.Mutation;
                if (!statementNodes.ContainsKey(kv.Key))
                    statementNodes.Add(kv.Key, kv.Value);
            }
        }
    }
}
