using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Helper;
using FASTER.core;

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
            Dictionary<string, NodeTree> entityTrees,
            Dictionary<string, NodeTree> modelTrees,
            Dictionary<string, string> sqlWhereStatement,
            IFasterKV<string, string> cache, 
            string cacheKey,
            string modelName)
        {
            var ctx = new SqlCompilationContext();
            var splitOnDapper = new OrderedDictionary<string, Type>();
            var aliases = new OrderedDictionary<string, string>();
            
            if (sqlWhereStatement.Count == 0)
            {
                SqlWhereCompiler.Compile(ctx, rootSelection, rootTree, wrapperEntityName, sqlWhereStatement);    
            }
            
            //Refactor with new alias feature
            // SqlOrderCompiler.Compile(ctx, entityTrees, rootSelection, wrapperEntityName, nodeDict);
            var selectResult = SqlSelectBuilder.HandleGraphQL(rootSelection, SqlNodeRegistry.EntityNodes, 
                SqlNodeRegistry.ModelNodes, entityTrees, modelTrees, SqlNodeRegistry.EntityNames, SqlNodeRegistry.ModelNames, 
                rootTree.Name, wrapperEntityName, cache, cacheKey, modelName);
            // ctx.SelectSql = selectResult.Item1;
            // SqlPagingCompiler.Compile(rootTree, ctx, rootSelection);
            //
            // var entityMapping = new Dictionary<string, Type>();
            //
            // for (var i = 0; i < selectResult.splitOnDapper.Count; i++)
            // {
            //     entityMapping.Add(selectResult.aliasesOrdered[i], selectResult.splitOnDapper.ElementAt(selectResult.splitOnDapper.Count - 1 - i).Value);
            // }

            return selectResult;

            //     new SqlStructure
            // {
            //     SqlQuery = ctx.SelectSql,
            //     Pagination = ctx.Pagination,
            //     HasTotalCount = ctx.Pagination.TotalRecordCount.RecordCount > 0,
            //     HasPagination = ctx.Pagination.TotalPageRecords.PageRecords > 0,
            //     // SplitOnDapper = selectResult.Item2,
            //     Aliases = entityTrees.Select(entity => entity.Value.Alias).ToList(),
            //     SqlNodes = [..edgeDict.Values, ..nodeDict.Values],
            //     EntityMapping = entityMapping.Reverse().ToDictionary(),
            //     EntityTrees = entityTrees,
            //     ModelTrees = modelTrees
            // };
        }
    }

}