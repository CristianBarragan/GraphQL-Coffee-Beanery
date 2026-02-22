namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public interface IMappingRegistration
    {
        Dictionary<string, NodeMap> Register();
    }    
}