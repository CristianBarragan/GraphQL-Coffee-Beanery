using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlQueryCompiler
    {
        public static SqlStructure Compile(
            ISelection rootSelection,
            NodeTree rootTree,
            Dictionary<string, SqlNode> edgeDict,
            Dictionary<string, SqlNode> nodeDict)
        {
            var ctx = new SqlCompilationContext();

            SqlWhereCompiler.Compile(ctx, rootSelection, rootTree, nodeDict);
            SqlOrderCompiler.Compile(ctx, rootSelection, rootTree, nodeDict);
            SqlPagingCompiler.Compile(ctx, rootSelection);

            SqlTreeWalker.WalkQueryNode(rootTree, edgeDict, nodeDict, ctx);

            ctx.SelectSql = SqlSelectBuilder.Build(ctx, rootTree, nodeDict, edgeDict);

            return new SqlStructure
            {
                SqlQuery = ctx.SelectSql,
                SqlUpsert = ctx.UpsertSql
            };
        }
    }

}