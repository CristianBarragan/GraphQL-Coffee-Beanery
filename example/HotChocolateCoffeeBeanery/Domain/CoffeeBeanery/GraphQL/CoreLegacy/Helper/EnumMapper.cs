using System;
using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Helper
{
    public static class EnumMapper
    {
        public static object Convert(Enum value, Dictionary<Enum, Enum> map)
        {
            if (value == null || map == null) return null;
            if (map.TryGetValue(value, out var mapped)) return mapped;
            return null;
        }
    }
}