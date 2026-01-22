using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public static class SqlCompiler
    {
        public static SqlStructure Compile(
            ISelection rootSelection,
            NodeTree rootTree,
            Dictionary<string, SqlNode> mutationDict,
            Dictionary<string, SqlNode> edgeDict,
            Dictionary<string, SqlNode> nodeDict)
        {
            var ctx = new SqlCompilationContext();

            SqlWhereCompiler.Compile(ctx, rootSelection, rootTree, nodeDict);
            SqlOrderCompiler.Compile(ctx, rootSelection, rootTree, nodeDict);
            SqlPagingCompiler.Compile(ctx, rootSelection);

            SqlTreeWalker.Walk(rootTree, mutationDict, edgeDict, nodeDict, ctx);

            ctx.SelectSql = SqlSelectBuilder.Build(ctx, rootTree, nodeDict, edgeDict);

            return new SqlStructure
            {
                SqlQuery = ctx.SelectSql,
                SqlUpsert = ctx.UpsertSql
            };
        }
    }
}