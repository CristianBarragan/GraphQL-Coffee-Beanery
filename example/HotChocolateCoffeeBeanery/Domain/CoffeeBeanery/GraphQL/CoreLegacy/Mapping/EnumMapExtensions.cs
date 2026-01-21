namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public static class EnumMapExtensions
    {
        public static TEntityEnum MapToEntity<TModelEnum, TEntityEnum>(
            this EnumMap<TModelEnum, TEntityEnum> map,
            TModelEnum value)
            where TModelEnum : System.Enum
            where TEntityEnum : System.Enum =>
            map.ToEntity.TryGetValue(value, out var e) ? e : default;

        public static TModelEnum MapToModel<TModelEnum, TEntityEnum>(
            this EnumMap<TModelEnum, TEntityEnum> map,
            TEntityEnum value)
            where TModelEnum : System.Enum
            where TEntityEnum : System.Enum =>
            map.ToModel.TryGetValue(value, out var m) ? m : default;
    }
}