namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public interface IMappingRegistration
    {
        IReadOnlyDictionary<string, NodeMap> Register();
    }    
}