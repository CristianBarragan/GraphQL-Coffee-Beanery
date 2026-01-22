using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public static class EnumMapFactory
    {
        public static (Dictionary<string,string> from, Dictionary<string,string> to) Create<TModelEnum, TEntityEnum>(
            Dictionary<TModelEnum, TEntityEnum> from,
            Dictionary<TEntityEnum, TModelEnum> to)
        {
            var fromDict = new Dictionary<string, string>();
            var toDict = new Dictionary<string, string>();

            foreach (var kv in from)
                fromDict.Add(kv.Key.ToString(), kv.Value.ToString());

            foreach (var kv in to)
                toDict.Add(kv.Key.ToString(), kv.Value.ToString());

            return (fromDict, toDict);
        }
    }

}