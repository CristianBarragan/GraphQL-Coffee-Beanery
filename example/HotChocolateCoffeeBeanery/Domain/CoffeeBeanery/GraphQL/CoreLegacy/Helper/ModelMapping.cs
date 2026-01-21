using System;
using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Helper
{
    public class ModelMapping<TModel>
        where TModel : class
    {
        public Type ModelType { get; } = typeof(TModel);

        internal readonly Dictionary<Type, IEntityMap> _entityMaps = new();
        public IReadOnlyDictionary<Type, IEntityMap> EntityMaps => _entityMaps;

        public void AddEntityMap(IEntityMap map)
        {
            _entityMaps[map.EntityType] = map;
        }
    }
}