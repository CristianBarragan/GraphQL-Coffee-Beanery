using System;
using System.Collections.Concurrent;

namespace CoffeeBeanery.GraphQL.Core.Helper
{
    public static class BulkMapperCache
    {
        private static readonly ConcurrentDictionary<(Type, Type), Delegate> Cache
            = new();

        public static Action<TSrc, TDst> Get<TSrc, TDst>(
            PropertyMapping<TSrc, TDst>[] mappings)
        {
            var key = (typeof(TSrc), typeof(TDst));
            if (Cache.TryGetValue(key, out var d)) return (Action<TSrc, TDst>)d;

            var compiled = BulkMapper.Compile(mappings);
            Cache[key] = compiled;
            return compiled;
        }
    }
}