
// using CoffeeBeanery.GraphQL.Configuration;
using Database.Graph;

namespace Database.Entity;

public class Wrapper
{
    // [LinkKey("CustomerCustomerRelationshipEdge", "CustomerCustomerRelationshipKey")]
    // [LinkIdKey("CustomerCustomerRelationshipEdge", "Id")]
    // [JoinKey("CustomerCustomerRelationshipEdge","CustomerCustomerRelationshipId")]
    public List<CustomerCustomerRelationshipEdge> CustomerCustomerRelationshipEdge { get; set; }
}