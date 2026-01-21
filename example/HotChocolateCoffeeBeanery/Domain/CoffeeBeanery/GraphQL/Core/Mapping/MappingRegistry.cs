using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public static class MappingRegistry
    {
        private static readonly List<MappingDefinition> _definitions = new();

        public static IReadOnlyList<MappingDefinition> All => _definitions;

        public static void Register(MappingDefinition definition)
        {
            _definitions.Add(definition);
        }
        
        public static IReadOnlyList<object> GetAll()
        {
            return _definitions;
        }
    }
}
