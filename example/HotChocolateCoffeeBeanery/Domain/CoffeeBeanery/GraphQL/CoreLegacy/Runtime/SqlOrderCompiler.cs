using System.Text;
using CoffeeBeanery.GraphQL.Core.Helper;
using CoffeeBeanery.GraphQL.Core.Mapping;
using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlOrderCompiler
    {
        public static void Compile<D, S>(
            SqlCompilationContext ctx,
            ISelection rootSelection,
            IEntityTreeMap<D, S> entityTreeMap,
            IModelTreeMap<D, S> modelTreeMap,
            string rootEntityName,
            string wrapperEntityName)
            where D : class
            where S : class
        {
            var sb = new StringBuilder();

            foreach (var arg in rootSelection.SyntaxNode.Arguments)
            {
                if (arg.Name.Value.Contains("order"))
                {
                    var raw = arg.Value?.ToString() ?? "";
                    raw = raw.Trim('{', '}', ' ');

                    var parts = raw.Split(':').Select(p => p.Trim()).ToArray();
                    if (parts.Length == 2)
                    {
                        sb.Append(SqlGraphQLHelper.HandleSort(
                            entityTreeMap.DictionaryTree[rootEntityName],
                            parts[0],
                            parts[1],
                            modelTreeMap.LinkDictionaryTreeNode));
                    }
                }
            }

            var result = sb.ToString().TrimEnd(',', ' ');
            if (!string.IsNullOrEmpty(result))
            {
                ctx.OrderBy = result;
                ctx.HasSorting = true;
            }
        }
    }
}