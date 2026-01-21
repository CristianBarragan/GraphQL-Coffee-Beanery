using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public static class SqlNodeRegistry
    {
        private static readonly ConcurrentDictionary<string, SqlNode> All
            = new();

        public static void Register(Dictionary<string, SqlNode> dict)
        {
            foreach (var kv in dict)
                All[kv.Key] = kv.Value;
        }

        public static IReadOnlyDictionary<string, SqlNode> Nodes => All;
    }
}