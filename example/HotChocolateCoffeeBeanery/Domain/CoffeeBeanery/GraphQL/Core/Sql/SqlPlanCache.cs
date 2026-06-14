// using FASTER.core;
//
// namespace CoffeeBeanery.GraphQL.Core.Sql;
//
// public class SqlPlanCache
// {
//     private readonly IFasterKV<string, string> _cache;
//
//     public SqlPlanCache(IFasterKV<string, string> cache)
//     {
//         _cache = cache;
//     }
//
//     public bool TryGet(string key, out string sql)
//     {
//         return _cache.TryGetValue(new () { key = key }, out var value) && (sql = value) != null;
//     }
//
//     public void Set(string key, string sql)
//     {
//         _cache.Upsert(new () { key = key }, new () { value = sql });
//     }
// }