
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Helper;

public static class EnumMapExtensions
{
    public static TE MapToEntity<TM, TE>(this EnumMap<TM, TE> map, TM value)
        where TM : System.Enum
        where TE : System.Enum =>
        map.ToEntity.TryGetValue(value, out var eVal) ? eVal : default;

    public static TM MapToModel<TM, TE>(this EnumMap<TM, TE> map, TE value)
        where TM : System.Enum
        where TE : System.Enum =>
        map.ToModel.TryGetValue(value, out var mVal) ? mVal : default;
}
