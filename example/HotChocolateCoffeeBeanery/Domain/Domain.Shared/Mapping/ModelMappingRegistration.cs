using CoffeeBeanery.GraphQL.Core.Mapping;

namespace Domain.Shared.Mapping
{
    public class ModelMappingRegistration
    {
        private static readonly IMappingSet<CustomerMappingType>[] Sets =
        {
            new AccountMappingSet(),
            new ContactPointMappingSet(),
            new ContractMappingSet(),
            new CustomerBankingRelationshipMappingSet(),
            new CustomerCustomerRelationshipMappingSet(),
            new InnerCustomerMappingSet(),
            new OuterCustomerMappingSet(),
            new ProductMappingSet(),
            new TransactionMappingSet(),
            new CustomerCustomerEdgeMappingSet()
        };

        public void Register()
        {
            foreach (CustomerMappingType type in Enum.GetValues(typeof(CustomerMappingType)))
            {
                foreach (var set in Sets)
                {
                    set.Register(type);
                }
            }

            MappingRegistry.BuildDottedAliases();
        }
    }
    
    
    public enum CustomerMappingType {
        InnerCustomer,
        OuterCustomer
    }
    
    // public class ModelMappingRegistration
    // {
    //     public void Register()
    //     {
    //         new AccountMapping(nameof(CustomerMappingType.InnerCustomer)).Register();
    //         new ContactPointMapping(nameof(CustomerMappingType.InnerCustomer)).Register();
    //         new ContractMapping(nameof(CustomerMappingType.InnerCustomer)).Register();
    //         new CustomerBankingRelationshipMapping(nameof(CustomerMappingType.InnerCustomer)).Register();
    //         // new CustomerCustomerRelationshipEdgeMapping().Register();
    //         new CustomerCustomerRelationshipMapping(nameof(CustomerMappingType.InnerCustomer)).Register();
    //         new InnerCustomerMapping(nameof(CustomerMappingType.InnerCustomer)).Register();
    //         new ProductMapping(nameof(CustomerMappingType.InnerCustomer)).Register();
    //         new TransactionMapping(nameof(CustomerMappingType.InnerCustomer)).Register();
    //         new CustomerCustomerEdgeMapping(nameof(CustomerMappingType.InnerCustomer)).Register();
    //         
    //         //OuterCustomer
    //         new AccountMapping(nameof(CustomerMappingType.OuterCustomer)).Register();
    //         new ContactPointMapping(nameof(CustomerMappingType.OuterCustomer)).Register();
    //         new ContractMapping(nameof(CustomerMappingType.OuterCustomer)).Register();
    //         new CustomerBankingRelationshipMapping(nameof(CustomerMappingType.OuterCustomer)).Register();
    //         // new CustomerCustomerRelationshipEdgeMapping().Register();
    //         new CustomerCustomerRelationshipMapping(nameof(CustomerMappingType.OuterCustomer)).Register();
    //         new InnerCustomerMapping(nameof(CustomerMappingType.OuterCustomer)).Register();
    //         new ProductMapping(nameof(CustomerMappingType.OuterCustomer)).Register();
    //         new TransactionMapping(nameof(CustomerMappingType.OuterCustomer)).Register();
    //         new CustomerCustomerEdgeMapping(nameof(CustomerMappingType.OuterCustomer)).Register();
    //
    //         MappingRegistry.BuildDottedAliases();
    //     }
    // }
}