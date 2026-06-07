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
            SqlCompilationContext context,
            ISelection rootSelection,
            NodeTree rootTree,
            IFasterKV<string, string> cache,
            string cacheKey)
        {
            var sqlWhereStatement = new Dictionary<string, string>();
            var sqlOrderStatement = string.Empty;
            var statementNodes = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);
            
            var visitedModels = new List<string>();
            var visitedEntities = new List<string>();
            var hasSorting = false;
            var hasPagination = false;
            var hasTotalCount = false;
            var pagination = new Pagination();
            var modelSqlNodes = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);
            
            SqlSelectBuilder.GetFields(SqlNodeRegistry.ModelTrees, SqlNodeRegistry.EntityTrees, rootSelection.SyntaxNode.GetNodes()
                    .ToList().Last(a => a.Kind == SyntaxKind.SelectionSet).GetNodes().ToList().Last().GetNodes().Last().GetNodes().First(), 
                SqlNodeRegistry.EntityNodes, SqlNodeRegistry.ModelNodes, statementNodes, rootTree, visitedModels, 
                visitedEntities, SqlNodeRegistry.ModelNames, modelSqlNodes, false);

            SqlSelectBuilder.GetFields(SqlNodeRegistry.ModelTrees, SqlNodeRegistry.EntityTrees, rootSelection.SyntaxNode.GetNodes()
                    .ToList().Last(a => a.Kind == SyntaxKind.SelectionSet), SqlNodeRegistry.EntityNodes,
                SqlNodeRegistry.ModelNodes, statementNodes, rootTree, visitedModels, visitedEntities,
                SqlNodeRegistry.ModelNames, modelSqlNodes, true);
            
            SqlWhereCompiler.Compile(context, SqlNodeRegistry.ModelTrees, SqlNodeRegistry.EntityTrees, 
                modelSqlNodes, statementNodes,
                rootSelection, rootTree, 
                rootTree.Name, sqlWhereStatement);
            
            var selectResult = SqlSelectBuilder.HandleGraphQL(rootSelection, context, SqlNodeRegistry.EntityNodes, statementNodes, 
                sqlWhereStatement, SqlNodeRegistry.EntityTrees, SqlNodeRegistry.EntityNames, rootTree, cache, cacheKey);

            foreach (var argument in rootSelection.SyntaxNode.Arguments
                         .Where(a => !a.Name.Value.Matches("where")))
            {
                switch (argument.Name.ToString())
                {
                    case "first":
                        pagination.First = string.IsNullOrEmpty(argument.Value?.Value?.ToString())
                            ? 0
                            : int.Parse(argument.Value?.Value.ToString());
                        hasPagination = true;
                        break;
                    case "last":
                        pagination.Last = string.IsNullOrEmpty(argument.Value?.Value?.ToString())
                            ? 0
                            : int.Parse(argument.Value?.Value.ToString());
                        hasPagination = true;
                        break;
                    case "before":
                        pagination.Before = string.IsNullOrEmpty(argument.Value?.Value?.ToString())
                            ? ""
                            : argument.Value?.Value.ToString();
                        hasPagination = true;
                        break;
                    case "after":
                        pagination.After = string.IsNullOrEmpty(argument.Value?.Value?.ToString())
                            ? ""
                            : argument.Value?.Value.ToString();
                        hasPagination = true;
                        break;
                }
                
                if (argument.Name.ToString().Contains("order"))
                {
                    foreach (var orderNode in argument.GetNodes())
                    {
                        hasSorting = true;
                        sqlOrderStatement += SqlSelectBuilder.GetFieldsOrdering(SqlNodeRegistry.ModelTrees, orderNode,
                            rootTree, SqlNodeRegistry.EntityNodes);
                    }
                }
            }
            
            if (hasPagination || hasSorting)
            {
                selectResult.HasPagination = true;
                // selectResult.SqlQuery = SqlHelper.HandleQueryClause(rootTree, selectResult.SqlQuery, sqlOrderStatement, pagination, hasTotalCount);
                selectResult.Pagination = pagination;
            }
            
            selectResult.SqlNodesApplied = statementNodes;
            
            return selectResult;
        }
    }

}