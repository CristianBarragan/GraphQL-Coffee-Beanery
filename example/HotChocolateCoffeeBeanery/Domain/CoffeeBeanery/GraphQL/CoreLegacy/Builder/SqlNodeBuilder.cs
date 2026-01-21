using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Builder
{
    public static class SqlNodeBuilder
    {
        public static Dictionary<string, SqlNode> BuildFromModel<TModel>()
            where TModel : class
        {
            var map = MappingRegistry.Get<TModel>();
            var dict = new Dictionary<string, SqlNode>();

            foreach (var kv in map.EntityMaps)
            {
                var etype = kv.Key;
                var emap = kv.Value;

                // fields
                foreach (var fm in emap.FieldMaps)
                {
                    var ename = etype.Name;
                    var propName = ExpressionHelpers.GetPropertyName(fm.Destination);
                    var key = $"{ename}~{propName}";
                    if (!dict.ContainsKey(key))
                        dict[key] = new SqlNode
                        {
                            EntityName = ename,
                            PropertyName = propName
                        };
                }

                // links
                foreach (var lk in emap.LinkMaps)
                {
                    var ename = etype.Name;
                    var destKey = ExpressionHelpers.GetPropertyName(lk.EntityKey);

                    var key = $"{ename}~{destKey}";
                    if (!dict.ContainsKey(key))
                        dict[key] = new SqlNode
                        {
                            EntityName = ename,
                            PropertyName = destKey
                        };

                    dict[key].LinkKeys.Add(new LinkKey
                    {
                        From = ExpressionHelpers.GetPropertyName(lk.SourceKey),
                        To = destKey
                    });
                }

                // upsert keys
                foreach (var up in emap.UpsertKeys)
                {
                    foreach (var node in dict.Values.Where(n => n.EntityName == etype.Name))
                        node.UpsertKeys.Add(up);
                }

                // enums
                foreach (var emu in emap.EnumMaps)
                {
                    foreach (var node in dict.Values.Where(n => n.EntityName == etype.Name))
                        node.EnumMaps.Add(emu);
                }
            }

            return dict;
        }
    }
}
