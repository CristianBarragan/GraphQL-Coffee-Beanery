using System;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SqlQueryCompiler
    {
        public static SqlSelectResult CompileQuery<TModel>(
            ISelection rootSelection,
            NodeTree rootNode)
            where TModel : class
        {
            var ctx = new SqlCompilationContext();
            var (select, edge, mutation) = SqlNodeResolver.ResolveFromSelection<TModel>(rootSelection, rootNode.Name, false);

            SqlWhereBuilder.BuildWhere<TModel>(ctx, rootSelection, select, rootNode.Name);
            SqlOrderByBuilder.BuildOrderBy(ctx, rootSelection, select, rootNode.Name);
            SqlPaginationBuilder.BuildPagination(ctx, rootSelection);

            return SqlSelectGenerator.BuildSelect(ctx, select, rootNode);
        }
    }
}