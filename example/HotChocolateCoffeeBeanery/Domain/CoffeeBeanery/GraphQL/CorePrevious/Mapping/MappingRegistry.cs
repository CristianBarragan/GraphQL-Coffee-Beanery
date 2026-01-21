using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class EntityMappingRegistry
    {
        private readonly Dictionary<string, EntityMapping> _maps = new();

        public void Register(EntityMapping map)
            => _maps[map.Entity] = map;

        public EntityMapping Get(string entity)
            => _maps[entity];
    }
}