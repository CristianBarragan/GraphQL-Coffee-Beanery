using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public static class SqlMutationCompiler
    {
        public static void Compile(
            SqlCompilationContext context,
            ISelection rootSelection,
            NodeTree rootTree,
            Dictionary<string, string> sqlWhereStatement,
            Dictionary<string, NodeTree> modelTrees,
            Dictionary<string, NodeTree> entityTrees,
            Dictionary<string, SqlNode> modelSqlNodes,
            Dictionary<string, SqlNode> entitySqlNodes,
            List<string> models,
            List<string> entities)
        {
            var sqlUpsertStatementNodes =
                new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);

            var statements = new List<string>();
            var selectStatements = new List<string>();

            var mutationArgument = GetMutationArgument(rootSelection);

            foreach (var mutationNode in EnumerateMutations(mutationArgument))
            {
                ProcessMutation(
                    mutationNode,
                    rootTree,
                    sqlWhereStatement,
                    modelTrees,
                    entityTrees,
                    modelSqlNodes,
                    entitySqlNodes,
                    sqlUpsertStatementNodes,
                    models,
                    entities,
                    statements,
                    selectStatements);
            }

            statements.Reverse();

            context.UpsertSql =
                string.Join(";",
                    statements
                        .Concat(selectStatements)
                        .Where(x => !string.IsNullOrWhiteSpace(x)));
        }

        private static void ProcessMutation(
            ISyntaxNode mutationNode,
            NodeTree rootTree,
            Dictionary<string, string> sqlWhereStatement,
            Dictionary<string, NodeTree> modelTrees,
            Dictionary<string, NodeTree> entityTrees,
            Dictionary<string, SqlNode> modelSqlNodes,
            Dictionary<string, SqlNode> entitySqlNodes,
            Dictionary<string, SqlNode> sqlUpsertStatementNodes,
            List<string> models,
            List<string> entities,
            List<string> statements,
            List<string> selectStatements)
        {
            SqlSelectBuilder.GetMutations(
                modelTrees,
                mutationNode,
                modelSqlNodes,
                entitySqlNodes,
                sqlUpsertStatementNodes,
                rootTree,
                string.Empty,
                models);

            SqlHelper.GenerateUpsertStatements(
                entityTrees,
                sqlUpsertStatementNodes,
                rootTree,
                entities,
                sqlWhereStatement,
                new List<string>(),
                statements,
                selectStatements);
        }

        private static ISyntaxNode GetMutationArgument(
            ISelection rootSelection)
        {
            return rootSelection.SyntaxNode
                .GetNodes()
                .First(x => x.Kind == SyntaxKind.Argument);
        }

        private static IEnumerable<ISyntaxNode> EnumerateMutations(
            ISyntaxNode mutationArgument)
        {
            if (mutationArgument.ToString().StartsWith("["))
            {
                foreach (var mutationNode in mutationArgument
                             .GetNodes()
                             .Last()
                             .GetNodes())
                {
                    yield return mutationNode;
                }

                yield break;
            }

            yield return mutationArgument;
        }
    }
}