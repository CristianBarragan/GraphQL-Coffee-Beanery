using System;
using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public class ModelMapping<TModel>
        where TModel : class
    {
        public Type ModelType { get; } = typeof(TModel);

        private readonly Dictionary<Type, IEntityMap> _entityMaps = new();
        public IReadOnlyDictionary<Type, IEntityMap> EntityMaps => _entityMaps;

        public void AddEntityMap(IEntityMap map)
        {
            _entityMaps[map.EntityType] = map;
        }
    }
}