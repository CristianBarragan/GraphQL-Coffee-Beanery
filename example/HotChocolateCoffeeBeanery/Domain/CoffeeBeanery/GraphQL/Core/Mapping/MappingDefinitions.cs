using System;
using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class MappingDefinition
    {
        internal List<object> EntityMaps { get; } = new();
        internal List<object> EnumMaps { get; } = new();

        public void AddEntityMap<TModel, TEntity>(EntityMap<TModel, TEntity> map)
            where TModel : class
            where TEntity : class
        {
            EntityMaps.Add(map);
        }

        public void AddEnumMap<TModelEnum, TEntityEnum>(EnumMapWrapper<TModelEnum, TEntityEnum> map)
            where TModelEnum : struct, Enum
            where TEntityEnum : struct, Enum
        {
            EnumMaps.Add(map);
        }

        public static EnumMapWrapper<TModelEnum, TEntityEnum> Create<TModelEnum, TEntityEnum>(
            Dictionary<TModelEnum, TEntityEnum> toEntity,
            Dictionary<TEntityEnum, TModelEnum> toModel)
            where TModelEnum : struct, Enum
            where TEntityEnum : struct, Enum
        {
            return new EnumMapWrapper<TModelEnum, TEntityEnum>(toEntity, toModel);
        }
    }
}