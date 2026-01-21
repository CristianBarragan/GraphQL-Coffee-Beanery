using System;
using System.Linq;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public static class EnumMapExtensions
    {
        public static object ToEntity(
            this EnumMapWrapper wrapper,
            object modelValue)
        {
            if (wrapper.ModelType != modelValue.GetType())
                throw new InvalidOperationException("Enum type mismatch.");

            var toEntity = (System.Collections.IDictionary)wrapper.ToEntityMap;
            return toEntity[modelValue];
        }

        public static object ToModel(
            this EnumMapWrapper wrapper,
            object entityValue)
        {
            if (wrapper.EntityType != entityValue.GetType())
                throw new InvalidOperationException("Enum type mismatch.");

            var toModel = (System.Collections.IDictionary)wrapper.ToModelMap;
            return toModel[entityValue];
        }
    }
}