using System;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public static class MappingBuilder
    {
        public static MappingBuilderWrapper<TModel> Create<TModel>(Action<MappingBuilderWrapper<TModel>> configure)
        {
            var wrapper = new MappingBuilderWrapper<TModel>();
            configure(wrapper);
            return wrapper;
        }
    }

    public class MappingBuilderWrapper<TModel>
    {
        private readonly MappingDefinition _definition = new();

        public void AddEntityMap<TEnt>(EntityMap<TModel, TEnt> map)
        {
            _definition.EntityMaps.Add(map);
            foreach (var enumMap in map.EnumMaps)
                _definition.EnumMaps.Add(enumMap);
        }

        public MappingDefinition Build() => _definition;
    }
}