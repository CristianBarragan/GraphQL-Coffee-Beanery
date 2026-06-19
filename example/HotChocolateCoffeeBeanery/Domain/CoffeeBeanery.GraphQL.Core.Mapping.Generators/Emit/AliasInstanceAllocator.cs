using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Mapping.Generators.Emit
{
    internal static class AliasInstanceAllocator
    {
        private static readonly Dictionary<string, int> _map = new();

        public static string GetRootAlias(string modelName)
        {
            if (!_map.TryGetValue(modelName, out var i))
                i = 1;

            _map[modelName] = i + 1;

            return $"{modelName}#{i}";
        }
    }
}