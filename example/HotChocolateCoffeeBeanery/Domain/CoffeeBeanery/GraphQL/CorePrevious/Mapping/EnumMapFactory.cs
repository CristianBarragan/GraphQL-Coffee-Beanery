namespace CoffeeBeanery.GraphQL.Core.Mapping;

public static class EnumMapFactory
{
    public static EnumMapWrapper Create<TModelEnum, TEntityEnum>(
        Dictionary<TModelEnum, TEntityEnum> toEntity,
        Dictionary<TEntityEnum, TModelEnum> toModel)
    {
        return new EnumMapWrapper
        {
            ModelType = typeof(TModelEnum),
            EntityType = typeof(TEntityEnum),
            ToEntityMap = toEntity,
            ToModelMap = toModel
        };
    }
}
