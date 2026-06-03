using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Helper;
using FASTER.core;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlQueryCompiler
    {
        public static SqlStructure Compile(
            ISelection rootSelection,
            NodeTree rootTree,
            Dictionary<string, SqlNode> sqlNodes,
            Dictionary<string, NodeTree> entityTrees,
            Dictionary<string, NodeTree> modelTrees,
            IFasterKV<string, string> cache,
            string cacheKey)
        {
            var ctx = new SqlCompilationContext();
            var splitOnDapper = new OrderedDictionary<string, Type>();
            var aliases = new OrderedDictionary<string, string>();
            var sqlWhereStatement = new Dictionary<string, string>();
            var statementNodes = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);
            
            if (sqlWhereStatement.Count == 0)
            {
                SqlWhereCompiler.Compile(ctx, rootSelection, rootTree, rootTree.Name, sqlWhereStatement);    
            }
            
            var visitedModels = new List<string>();
            var visitedEntities = new List<string>();
            
            SqlSelectBuilder.GetFields(SqlNodeRegistry.ModelTrees, SqlNodeRegistry.EntityTrees, rootSelection.SyntaxNode.GetNodes()
                    .ToList().Last(a => a.Kind == SyntaxKind.SelectionSet).GetNodes().ToList().Last().GetNodes().Last().GetNodes().First(), 
                SqlNodeRegistry.EntityNodes, SqlNodeRegistry.ModelNodes, statementNodes, rootTree, new NodeTree(), visitedModels, 
                visitedEntities, SqlNodeRegistry.ModelNames, SqlNodeRegistry.EntityNames, true);

            SqlSelectBuilder.GetFields(SqlNodeRegistry.ModelTrees, SqlNodeRegistry.EntityTrees, rootSelection.SyntaxNode.GetNodes().ToList().Last(a => a.Kind == SyntaxKind.SelectionSet), SqlNodeRegistry.EntityNodes,
                SqlNodeRegistry.ModelNodes, statementNodes, rootTree, new NodeTree(), visitedModels, visitedEntities,
                SqlNodeRegistry.ModelNames, SqlNodeRegistry.EntityNames, false);
            
            //Refactor with new alias feature
            // SqlOrderCompiler.Compile(ctx, entityTrees, rootSelection, wrapperEntityName, nodeDict);
            var selectResult = SqlSelectBuilder.HandleGraphQL(sqlNodes, statementNodes, sqlWhereStatement, entityTrees, 
                SqlNodeRegistry.EntityNames, rootTree, cache, cacheKey);
            
            var hasTotalCount = false;

            /*
             * TODO Handle query clause
             */
            
            // if (hasPagination || hasSorting)
            // {
            //     rootNodeTree = entityTrees[rootEntityName];
            //     // Query Where, Sort, and Pagination
            //     sqlSelectStatement = SqlHelper.HandleQueryClause(rootNodeTree, sqlSelectStatement,
            //         sqlOrderStatement, pagination, hasTotalCount);
            // }

            return selectResult;
        }
    }

}