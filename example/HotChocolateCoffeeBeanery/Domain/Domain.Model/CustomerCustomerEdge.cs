namespace Domain.Model;

public class CustomerCustomerEdge
{
    public Guid? CustomerCustomerRelationshipKey { get; set; }
    
    public CustomerCustomerRelationship? CustomerCustomerRelationship { get; set; }

    public Guid? OuterCustomerKey { get; set; }

    public Customer? OuterCustomer { get; set; }

    public Guid? InnerCustomerKey { get; set; }
    
    public Customer? InnerCustomer { get; set; }
    
    public string? Clause { get; set; }
    
    public int? LevelDepth { get; set; }
    
    public LevelDirection? LevelDirection { get; set; }
    
    public GraphType? GraphType { get; set; }
}