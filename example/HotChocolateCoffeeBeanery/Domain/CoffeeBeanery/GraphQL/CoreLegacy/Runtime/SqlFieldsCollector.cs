using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.Mapping;
using HotChocolate.Language;
using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlFieldsCollector
    {
        public static void Collect<D, S>(
            SqlCompilationContext ctx,
            ISelection rootSelection,
            IEntityTreeMap<D, S> entityTreeMap,
            IModelTreeMap<D, S> modelTreeMap,
            string rootEntityName,
            string wrapperEntityName,
            bool transformedToParent)
            where D : class
            where S : class
        {
            var collected = new Dictionary<string, SqlNode>();

            CollectFromSyntax(
                rootSelection.SyntaxNode,
                entityTreeMap,
                modelTreeMap,
                collected,
                rootEntityName);

            foreach (var kv in collected)
                ctx.UpsertNodes[kv.Key] = kv.Value;
        }

        private static void CollectFromSyntax<D, S>(
            ISyntaxNode node,
            IEntityTreeMap<D, S> entityTreeMap,
            IModelTreeMap<D, S> modelTreeMap,
            Dictionary<string, SqlNode> collected,
            string currentEntity)
            where D : class
            where S : class
        {
            foreach (var child in node.GetNodes())
            {
                var fieldName = child.ToString().Split('{')[0].Trim();

                var key = $"{currentEntity}~{fieldName}";
                if (modelTreeMap.LinkDictionaryTreeNode.TryGetValue(key, out var modelSql))
                {
                    var match = entityTreeMap.LinkDictionaryTreeNode
                        .Values
                        .FirstOrDefault(n =>
                            n.LinkKeys.Any(lk =>
                                lk.To.Split('~')[1] == modelSql.Column));

                    if (match != null)
                    {
                        match.Value = modelSql.Value;
                        var unique = $"{match.Entity}~{match.Column}";
                        if (!collected.ContainsKey(unique))
                            collected.Add(unique, match);

                        CollectFromSyntax(
                            child,
                            entityTreeMap,
                            modelTreeMap,
                            collected,
                            match.Entity);
                    }
                }
            }
        }
    }
}
