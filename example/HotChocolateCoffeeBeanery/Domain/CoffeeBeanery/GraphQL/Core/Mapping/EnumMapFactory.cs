using System;
using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public static class EnumMapFactory
    {
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