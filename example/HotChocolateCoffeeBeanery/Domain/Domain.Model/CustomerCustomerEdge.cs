
// using CoffeeBeanery.GraphQL.Configuration;
// using CoffeeBeanery.GraphQL.Helper;
// using CoffeeBeanery.GraphQL.Model;

// using CoffeeBeanery.GraphQL.Configuration;

namespace Domain.Model;

public class CustomerCustomerEdge
{
    // [GraphKey("CustomerCustomerRelationshipEdge")]
    // [LinkBusinessKey("CustomerCustomerRelationship","CustomerCustomerRelationshipId")]
    public Guid? CustomerCustomerRelationshipKey { get; set; }
    
    public CustomerCustomerRelationship? CustomerCustomerRelationship { get; set; }

    // [GraphKey("CustomerCustomerRelationshipEdge")]
    // [LinkBusinessKey("Customer","OuterCustomerId")]
    public Guid? OuterCustomerKey { get; set; }

    public Customer? OuterCustomer { get; set; }

    // [GraphKey("CustomerCustomerRelationshipEdge")]
    // [LinkBusinessKey("Customer","InnerCustomerId")]
    public Guid? InnerCustomerKey { get; set; }
    
    public Customer? InnerCustomer { get; set; }
    
    public string? Clause { get; set; }
    
    public int? LevelDepth { get; set; }
    
    public LevelDirection? LevelDirection { get; set; }
    
    public GraphType? GraphType { get; set; }
}