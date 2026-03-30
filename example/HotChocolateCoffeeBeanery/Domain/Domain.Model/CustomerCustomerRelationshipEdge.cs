
namespace Domain.Model;

public class CustomerCustomerRelationshipEdge
{
    public string? CustomerCustomerRelationshipKey { get; set; }
    
    public string? InnerCustomerKey { get; set; }
    
    public string? OuterCustomerKey { get; set; }
    
    public string? Clause { get; set; }
    
    public int? LevelDepth { get; set; }
    
    public LevelDirection? LevelDirection { get; set; }
    
    public GraphType? GraphType { get; set; }
    
}