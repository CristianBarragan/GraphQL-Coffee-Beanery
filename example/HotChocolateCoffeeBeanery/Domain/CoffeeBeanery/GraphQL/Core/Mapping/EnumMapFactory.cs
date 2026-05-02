using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public static class EnumMapFactory
    {
        public static (List<KeyValuePair<string, (string, int)>> from, List<KeyValuePair<string, (string, int)>> to) Create(
            List<KeyValuePair<string, (string, int)>> from,
            List<KeyValuePair<string, (string, int)>> to)
        {
            var fromDict = new List<KeyValuePair<string, (string, int)>>();
            var toDict = new List<KeyValuePair<string, (string, int)>>();

            foreach (var kv in from)
                fromDict.Add(new KeyValuePair<string, (string, int)>(kv.Key.ToString(), (kv.Value.Item1.ToString(), kv.Value.Item2)));

            foreach (var kv in to)
                toDict.Add(new KeyValuePair<string, (string, int)>(kv.Key.ToString(), (kv.Value.Item1.ToString(), kv.Value.Item2)));

            return (fromDict, toDict);
        }
    }

}