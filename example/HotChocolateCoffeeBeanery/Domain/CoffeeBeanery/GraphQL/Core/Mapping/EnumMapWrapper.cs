using System;
using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class EnumMapWrapper<TModelEnum, TEntityEnum>
        where TModelEnum : struct, Enum
        where TEntityEnum : struct, Enum
    {
        public Type ModelType { get; init; }
        public Type EntityType { get; init; }

        public IReadOnlyDictionary<TModelEnum, TEntityEnum> ToEntityMap { get; init; }
        public IReadOnlyDictionary<TEntityEnum, TModelEnum> ToModelMap { get; init; }

        public EnumMapWrapper(
            IReadOnlyDictionary<TModelEnum, TEntityEnum> toEntity,
            IReadOnlyDictionary<TEntityEnum, TModelEnum> toModel)
        {
            ModelType = typeof(TModelEnum);
            EntityType = typeof(TEntityEnum);

            ToEntityMap = toEntity;
            ToModelMap = toModel;
        }
    }
}
