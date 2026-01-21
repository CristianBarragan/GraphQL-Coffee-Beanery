using System.Text;
using CoffeeBeanery.GraphQL.CoreNew.Sql;

namespace CoffeeBeanery.GraphQL.CoreNew.Compiler
{
    public static class SqlMutationGeneratorNew
    {
        public static string BuildUpserts(
            SqlCompilationContext ctx,
            Dictionary<string, SqlNode> nodes,
            string rootEntity)
        {
            var sb = new StringBuilder();
            sb.AppendLine("LOAD 'age';");
            sb.AppendLine("SET search_path = ag_catalog, \"$user\", public;");

            foreach (var n in nodes.Values.Where(x => x.SqlNodeType == SqlNodeType.Mutation))
            {
                if (!string.IsNullOrEmpty(n.Cypher))
                {
                    sb.AppendLine(GenerateCypherUpsert(n));
                }
                else
                {
                    sb.AppendLine(GenerateRelationalUpsert(n));
                }
            }

            return sb.ToString();
        }

        private static string GenerateRelationalUpsert(SqlNode n)
        {
            return $"INSERT INTO \"{n.EntityName}\" (\"{n.ColumnName}\") " +
                   $"VALUES ('{n.Value}') " +
                   $"ON CONFLICT DO UPDATE SET \"{n.ColumnName}\" = '{n.Value}';";
        }

        private static string GenerateCypherUpsert(SqlNode n)
        {
            return $@"WITH graph_data AS (SELECT * FROM cypher('{n.Key}', $$ {n.Cypher} $$) AS data)
                        SELECT * FROM graph_data;";
        }
    }
}
