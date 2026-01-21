using System.Text;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SqlMutationPlanner
    {
        public static void Plan(
            string entity,
            Dictionary<string, object> values,
            SqlCompilationContext ctx)
        {
            foreach (var kv in values)
            {
                ctx.MutationNodes.Add(
                    $"{entity}.{kv.Key}",
                    new MutationNode
                    {
                        Entity = entity,
                        Column = kv.Key,
                        Parameter = $"@{kv.Key}"
                    });
            }
        }
        
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
                if (n.IsGraph)
                {
                    sb.AppendLine(GenerateGraphUpsert(n));
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

        private static string GenerateGraphUpsert(SqlNode n)
        {
            return $@"WITH graph_data AS (SELECT * FROM cypher('{n.Key}', $$ {n.Cypher} $$) AS data)
                        SELECT * FROM graph_data;";
        }
    }
}