using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public class SqlMutationCompiler
    {
        public static SqlStructure Compile(
            ISelection rootSelection,
            NodeTree rootTree,
            Dictionary<string, SqlNode> mutationDict,
            Dictionary<string, SqlNode> edgeDict,
            Dictionary<string, SqlNode> nodeDict)
        {
            var ctx = new SqlCompilationContext();

            // Where, Order and Paging are not used for mutation (optional)
            SqlWhereCompiler.Compile(ctx, rootSelection, rootTree, nodeDict);
            SqlOrderCompiler.Compile(ctx, rootSelection, rootTree, nodeDict);
            SqlPagingCompiler.Compile(ctx, rootSelection);

            // Walk tree to collect select columns and mutation fields
            SqlTreeWalker.Walk(rootTree, mutationDict, edgeDict, nodeDict, ctx);

            // Build select statement (to return inserted/updated values)
            ctx.SelectSql = SqlSelectBuilder.Build(ctx, rootTree, nodeDict, edgeDict);

            // Build upsert statement (INSERT/UPDATE)
            ctx.UpsertSql = SqlUpsertBuilder.BuildUpsert(
                rootTree,
                mutationDict,
                edgeDict,
                nodeDict);

            return new SqlStructure
            {
                SqlQuery = ctx.SelectSql,
                SqlUpsert = ctx.UpsertSql
            };
        }
    }
}