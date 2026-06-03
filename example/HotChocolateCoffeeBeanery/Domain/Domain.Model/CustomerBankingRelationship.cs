namespace Domain.Model;

public class CustomerBankingRelationship
{
    
    public Guid? CustomerBankingRelationshipKey { get; set; }
    
    public Guid? CustomerKey { get; set; }
    
    public Contract? Contract { get; set; }
}