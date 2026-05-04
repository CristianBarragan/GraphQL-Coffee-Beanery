namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public interface IMappingSet<TEnum>
        where TEnum : Enum
    {
        void Register(TEnum type);
    }
}