using System;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class MappingBuilder<TModel>
        where TModel : class
    {
        private readonly MappingDefinition _definition = new();

        public MappingBuilder<TModel> Configure(Action<MappingBuilder<TModel>> config)
        {
            config(this);
            return this;
        }

        public MappingBuilder<TModel> AddEntityMap<TEntity>(EntityMap<TModel, TEntity> map)
            where TEntity : class
        {
            _definition.AddEntityMap(map);
            return this;
        }

        public MappingBuilder<TModel> AddEnumMap<TModelEnum, TEntityEnum>(EnumMapWrapper<TModelEnum, TEntityEnum> map)
            where TModelEnum : struct, Enum
            where TEntityEnum : struct, Enum
        {
            _definition.AddEnumMap(map);
            return this;
        }

        public static MappingBuilder<TModel> Create(Action<MappingBuilder<TModel>> configure)
        {
            var builder = new MappingBuilder<TModel>();
            configure(builder);
            return builder;
        }

        public MappingDefinition Build()
        {
            return _definition;
        }
    }
}