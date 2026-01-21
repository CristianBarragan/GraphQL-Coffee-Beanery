using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.SqlResolver
{
    public static class SqlNodeResolver
    {
        public static SqlNode Resolve(
            Dictionary<string, SqlNode> dict,
            string nodeKey)
        {
            dict.TryGetValue(nodeKey, out var node);
            return node;
        }

        public static IEnumerable<SqlNode> GraphNodes(
            Dictionary<string, SqlNode> dict) =>
            dict.Values.Where(n => n.LinkKeys.Count > 0);
    }
}