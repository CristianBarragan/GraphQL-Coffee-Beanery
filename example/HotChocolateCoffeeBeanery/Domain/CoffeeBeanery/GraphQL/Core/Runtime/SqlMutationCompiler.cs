using System.Collections.Generic;
using System.Text;
using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Helper;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public class SqlMutationCompiler
    {
        public static string Compile(
            ISelection rootSelection, NodeTree rootTree, Dictionary<string, string> sqlWhereStatement,
            Dictionary<string, NodeTree> modelTrees, Dictionary<string, NodeTree> entityTrees, Dictionary<string, SqlNode> modelSqlNodes, Dictionary<string, SqlNode> entitySqlNodes,
            List<string> models, List<string> entities
            )
        {
            var sqlUpsertBuilder       = new StringBuilder();
            var sqlSelectUpsertBuilder = new StringBuilder();

            // if (sqlWhereStatement.Count == 0)
            //     SqlWhereCompiler.Compile(modelTrees, entityTrees, modelSqlNodes, rootSelection, rootTree, rootTree.Alias, sqlWhereStatement);
            
            var sqlUpsertStatementNodes = new Dictionary<string, SqlNode>();
            
            var mutationNodeToProcess = rootSelection.SyntaxNode.GetNodes().First(a => a.Kind == SyntaxKind.Argument);

            var nodeTreeRoot = new NodeTree();
            nodeTreeRoot.Name = string.Empty;
            var statements = new List<string>();
            var selectStatements = new List<string>();
            
            if (mutationNodeToProcess.ToString().StartsWith("["))
            {
                foreach (var mutationNode in mutationNodeToProcess.GetNodes().Last().GetNodes())
                {
                    SqlSelectBuilder.GetMutations(modelTrees, mutationNode,
                        modelSqlNodes, entitySqlNodes,
                        sqlUpsertStatementNodes, rootTree, string.Empty,
                        models);
                    
                    SqlHelper.GenerateUpsertStatements(entityTrees, sqlUpsertStatementNodes, rootTree, entities,
                        sqlWhereStatement, new List<string>(), statements, selectStatements);
                }
            }
            else
            {
                SqlSelectBuilder.GetMutations(modelTrees, mutationNodeToProcess,
                    modelSqlNodes, entitySqlNodes,
                    sqlUpsertStatementNodes, rootTree, string.Empty,
                    models);
                
                SqlHelper.GenerateUpsertStatements(entityTrees, sqlUpsertStatementNodes, rootTree, entities,
                    sqlWhereStatement, new List<string>(), statements, selectStatements);
            }
            statements.Reverse();
            return string.Join(";", statements) + " ; " + string.Join(";", selectStatements);
        }
    }
}