using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public class SqlMutationCompiler
    {
        public static void Compile(SqlCompilationContext context,
            ISelection rootSelection, NodeTree rootTree, Dictionary<string, string> sqlWhereStatement,
            Dictionary<string, NodeTree> modelTrees, Dictionary<string, NodeTree> entityTrees, Dictionary<string, SqlNode> modelSqlNodes, Dictionary<string, SqlNode> entitySqlNodes,
            List<string> models, List<string> entities
            )
        {
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
            context.UpsertSql = string.Join(";", statements) + " ; " + string.Join(";", selectStatements);
        }
    }
}