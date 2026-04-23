using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Helper;

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
            Dictionary<string, NodeTree> trees,
            Dictionary<string, string> sqlWhereStatement,
            bool transformedToParent)
        {
            var ctx = new SqlCompilationContext();
            var splitOnDapper = new OrderedDictionary<string, Type>();
            var aliases = new OrderedDictionary<string, string>();
            
            if (sqlWhereStatement.Count == 0)
            {
                SqlWhereCompiler.Compile(ctx, rootSelection, rootTree, wrapperEntityName, sqlWhereStatement);    
            }
            
            //Refactor with new alias feature
            SqlOrderCompiler.Compile(ctx, trees, rootSelection, rootTree.Name, nodeDict);
            var selectResult = SqlSelectBuilder.Build(rootTree, nodeDict, edgeDict, wrapperEntityName, sqlWhereStatement, splitOnDapper, aliases, transformedToParent);
            ctx.SelectSql = selectResult.Item1;
            SqlPagingCompiler.Compile(rootTree, ctx, rootSelection);

            var entityMapping = new Dictionary<string, Type>();
            
            for (var i = 0; i < selectResult.splitOnDapper.Count; i++)
            {
                entityMapping.Add(selectResult.aliasesOrdered[i], selectResult.splitOnDapper.ElementAt(selectResult.splitOnDapper.Count - 1 - i).Value);
            }

            return new SqlStructure
            {
                SqlQuery = ctx.SelectSql,
                SqlUpsert = ctx.UpsertSql,
                Pagination = ctx.Pagination,
                HasTotalCount = ctx.Pagination.TotalRecordCount.RecordCount > 0,
                HasPagination = ctx.Pagination.TotalPageRecords.PageRecords > 0,
                SplitOnDapper = selectResult.Item2,
                Aliases = selectResult.Item3,
                SqlNodes = [..edgeDict.Values, ..nodeDict.Values],
                EntityMapping = entityMapping,
                Trees = trees
            };
        }
    }

}