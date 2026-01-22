using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlUpsertBuilder
    {
        public static string BuildUpsert(
            NodeTree tree,
            Dictionary<string, SqlNode> mutationDict,
            Dictionary<string, SqlNode> edgeDict,
            Dictionary<string, SqlNode> nodeDict)
        {
            var upsertSql = new List<string>();

            // --------------------
            // 1) NODE UPSERTS
            // --------------------
            foreach (var node in nodeDict.Values.Where(n => !n.IsGraph))
            {
                var columns = nodeDict.Values
                    .Where(x => x.Entity == node.Entity && !string.IsNullOrEmpty(x.Value))
                    .Select(x => $"\"{x.Column}\"")
                    .Distinct()
                    .ToList();

                if (!columns.Any()) continue;

                var values = nodeDict.Values
                    .Where(x => x.Entity == node.Entity && !string.IsNullOrEmpty(x.Value))
                    .Select(x => $"'{x.Value}'")
                    .Distinct()
                    .ToList();

                var conflictColumns = string.Join(", ",
                    node.UpsertKeys.Select(x => $"\"{x.Split('~')[1]}\""));

                var updateColumns = string.Join(", ",
                    nodeDict.Values
                        .Where(x => x.Entity == node.Entity && !string.IsNullOrEmpty(x.Value))
                        .Select(x => $"\"{x.Column}\" = EXCLUDED.\"{x.Column}\"")
                        .Distinct());

                var sql = $@"
                    INSERT INTO ""{tree.Schema}"".""{node.Entity}""
                    ({string.Join(",", columns)})
                    VALUES ({string.Join(",", values)})
                    ON CONFLICT ({conflictColumns})
                    DO UPDATE SET {updateColumns};";

                upsertSql.Add(sql);
            }


            // --------------------
            // 2) EDGE UPSERTS
            // --------------------
            foreach (var edge in edgeDict.Values)
            {
                // Find link keys
                var fromKey = edge.LinkKeys.FirstOrDefault(k => k.From.Contains(edge.Entity));
                var toKey = edge.LinkKeys.FirstOrDefault(k => k.To.Contains(edge.Entity));

                if (fromKey == null || toKey == null)
                    continue;

                var sql = $@"
                    INSERT INTO ""{tree.Schema}"".""{edge.Entity}""
                    (""{fromKey.To.Split('~')[1]}"", ""{toKey.To.Split('~')[1]}"")
                    VALUES ('{fromKey}', '{toKey}')
                    ON CONFLICT (""{fromKey.To.Split('~')[1]}"", ""{toKey.To.Split('~')[1]}"")
                    DO NOTHING;";

                upsertSql.Add(sql);
            }


            // --------------------
            // 3) GRAPH MERGE (NODE + EDGE)
            // --------------------
            // (A) Node MERGE
            foreach (var node in nodeDict.Values.Where(x => x.IsGraph))
            {
                var graphColumns = nodeDict.Values
                    .Where(x => x.Graph == node.Graph && !string.IsNullOrEmpty(x.Value))
                    .ToList();

                if (!graphColumns.Any()) continue;

                var props = string.Join(", ",
                    graphColumns.Select(x => $"{x.Column}: '{x.Value}'"));

                var sql = $@"
                    SELECT * FROM cypher('{node.Graph}', $$
                        MERGE (n:{node.Entity} {{ {props} }})
                    $$);";

                upsertSql.Add(sql);
            }

            // (B) Edge MERGE
            foreach (var edge in edgeDict.Values.Where(x => x.IsGraph))
            {
                var link = edge.LinkKeys.FirstOrDefault();
                if (link == null) continue;

                // MERGE (a)-[r:REL]->(b)
                var sql = $@"
                    SELECT * FROM cypher('{edge.Graph}', $$
                        MERGE (a:{link.From} {{ id: '{link.From}' }})
                        MERGE (b:{link.To} {{ id: '{link.To}' }})
                        MERGE (a)-[:{edge.Relationship}]->(b)
                    $$);";

                upsertSql.Add(sql);
            }

            return string.Join("\n", upsertSql);
        }
    }
}
