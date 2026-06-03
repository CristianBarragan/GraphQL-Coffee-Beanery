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
        // Key format:             "{alias}~{field}"          → [0]=alias  [1]=field
        // RelationshipKey format: "{model}~{entity}~{field}" → [0]=model  [1]=entity  [2]=field
        // UpsertKey format:       "{alias}~{entity}~{field}" → [0]=alias  [1]=entity  [2]=field

        public static SqlStructure Compile(
            ISelection rootSelection, NodeTree rootTree, Dictionary<string, string> sqlWhereStatement,
            Dictionary<string, NodeTree> modelTrees, Dictionary<string, NodeTree> entityTrees, Dictionary<string, SqlNode> modelSqlNodes, Dictionary<string, SqlNode> entitySqlNodes,
            List<string> models, List<string> entities
            )
        {
            var ctx                    = new SqlCompilationContext();
            var generatedQuery         = new List<string>();
            var sqlUpsertBuilder       = new StringBuilder();
            var sqlSelectUpsertBuilder = new StringBuilder();
            var mutationDict = new Dictionary<string, SqlNode>();
            var sqlUpsertStatement = string.Empty;

            if (sqlWhereStatement.Count == 0)
                SqlWhereCompiler.Compile(ctx, rootSelection, rootTree, rootTree.Alias, sqlWhereStatement);

            var entitiesProcessed = new List<string>();
            
            var nodeTreeKeyValuePair =
                SqlNodeRegistry.ModelTrees.FirstOrDefault(a => a.Value.ModelToEntityLinks[0].AliasTo.Matches(rootTree.Alias));

            if (nodeTreeKeyValuePair.Value == null)
            {
                nodeTreeKeyValuePair = SqlNodeRegistry.ModelTrees.FirstOrDefault(a => a.Value.ModelToEntityLinks[0].AliasFrom.Matches(rootTree.Alias));
            }
            
            var sqlUpsertStatementNodes = new Dictionary<string, SqlNode>();
            var visitedModels = new List<string>()
            {
                rootTree.ModelName
            };
            
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
                        new NodeTree(), models, entities, visitedModels);
                    
                    SqlHelper.GenerateUpsertStatements(entityTrees, sqlUpsertStatementNodes, rootTree, entities,
                        sqlWhereStatement, new List<string>(), statements, selectStatements);
                }
            }
            else
            {
                SqlSelectBuilder.GetMutations(modelTrees, mutationNodeToProcess,
                    modelSqlNodes, entitySqlNodes,
                    sqlUpsertStatementNodes, rootTree, string.Empty,
                    new NodeTree(), models, entities, visitedModels);
                
                SqlHelper.GenerateUpsertStatements(entityTrees, sqlUpsertStatementNodes, rootTree, entities,
                    sqlWhereStatement, new List<string>(), statements, selectStatements);
            }
            statements.Reverse();
            sqlUpsertStatement = string.Join(";", statements);
            sqlUpsertStatement += string.Join(";", selectStatements);

            ctx.UpsertSql = sqlUpsertBuilder.ToString() + " ; " + sqlSelectUpsertBuilder.ToString();

            return new SqlStructure { SqlUpsert = sqlUpsertStatement };
        }
    }
}