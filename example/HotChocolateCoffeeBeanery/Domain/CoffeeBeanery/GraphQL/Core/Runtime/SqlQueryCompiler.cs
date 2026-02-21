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
            Dictionary<string, SqlNode> nodeDict,
            string wrapperEntityName,
            Dictionary<string, string> sqlWhereStatement)
        {
            var ctx = new SqlCompilationContext();

            if (sqlWhereStatement.Count == 0)
            {
                SqlWhereCompiler.Compile(ctx, rootSelection, rootTree, wrapperEntityName, sqlWhereStatement);    
            }
            
            SqlOrderCompiler.Compile(ctx, rootSelection, rootTree, nodeDict);
            SqlPagingCompiler.Compile(ctx, rootSelection);

            ctx.SelectSql = SqlSelectBuilder.Build(rootTree, nodeDict, edgeDict, wrapperEntityName, sqlWhereStatement);

            return new SqlStructure
            {
                SqlQuery = ctx.SelectSql,
                SqlUpsert = ctx.UpsertSql
            };
        }
    }

}