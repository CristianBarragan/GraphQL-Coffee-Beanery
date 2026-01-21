using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class MappingDefinition
    {
        internal List<object> EntityMaps { get; } = new();
        internal List<EnumMapWrapper> EnumMaps { get; } = new();
    }
}