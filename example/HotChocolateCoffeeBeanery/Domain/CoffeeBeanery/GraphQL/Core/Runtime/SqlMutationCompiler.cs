using System.Collections.Generic;
using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public class SqlMutationCompiler
    {
        public static SqlStructure Compile(
            NodeTree rootTree,
            Dictionary<string, SqlNode> mutationDict)
        {
            var ctx = new SqlCompilationContext();

            // Where, Order and Paging are not used for mutation (optional)
            // SqlWhereCompiler.Compile(ctx, rootSelection, rootTree, nodeDict);
            // SqlOrderCompiler.Compile(ctx, rootSelection, rootTree, nodeDict);
            // SqlPagingCompiler.Compile(ctx, rootSelection);

            // Walk tree to collect select columns and mutation fields
            SqlTreeWalker.WalkMutationNode(rootTree, mutationDict, ctx);

            // Build select statement (to return inserted/updated values)
            // ctx.SelectSql = SqlSelectBuilder.Build(ctx, rootTree, nodeDict, edgeDict);

            // Build upsert statement (INSERT/UPDATE)
            ctx.UpsertSql = SqlUpsertBuilder.BuildUpsert(
                rootTree,
                mutationDict);

            return new SqlStructure
            {
                SqlUpsert = ctx.UpsertSql
            };
        }
    }
}