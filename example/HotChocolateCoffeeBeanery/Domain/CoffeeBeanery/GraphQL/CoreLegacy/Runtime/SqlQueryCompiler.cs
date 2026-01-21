using CoffeeBeanery.GraphQL.Core.Mapping;
using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlQueryCompiler
    {
        public static void CompileQuery<D, S>(
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
            SqlWhereCompiler.Compile(
                ctx,
                rootSelection,
                entityTreeMap,
                modelTreeMap,
                rootEntityName,
                wrapperEntityName);

            SqlOrderCompiler.Compile(
                ctx,
                rootSelection,
                entityTreeMap,
                modelTreeMap,
                rootEntityName,
                wrapperEntityName);

            SqlPagingCompiler.Compile(
                ctx,
                rootSelection);

            SqlFieldsCollector.Collect(
                ctx,
                rootSelection,
                entityTreeMap,
                modelTreeMap,
                rootEntityName,
                wrapperEntityName,
                transformedToParent);

            ctx.SelectSql = SqlSelectBuilder.Build(
                ctx,
                entityTreeMap.DictionaryTree,
                entityTreeMap.EntityNames,
                rootEntityName);
        }
    }
}