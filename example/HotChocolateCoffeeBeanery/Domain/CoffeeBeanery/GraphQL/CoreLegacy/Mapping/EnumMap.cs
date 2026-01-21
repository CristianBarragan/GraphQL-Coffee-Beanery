using System;
using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class EnumMap<TEnumModel, TEnumEntity> : IEnumMap
        where TEnumModel : Enum
        where TEnumEntity : Enum
    {
        public Dictionary<TEnumModel, TEnumEntity> ToEntity { get; set; }
        public Dictionary<TEnumEntity, TEnumModel> ToModel { get; set; }
    }
}