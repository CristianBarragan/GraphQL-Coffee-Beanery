using CoffeeBeanery.GraphQL.Core.Mapping;

namespace Domain.Shared.Mapping
{
    public class ModelMappingRegistration
    {
        public void Register()
        {
            new AccountMapping().Register();
            new ContactPointMapping().Register();
            new ContractMapping().Register();
            new CustomerBankingRelationshipMapping().Register();
            // new CustomerCustomerRelationshipEdgeMapping().Register();
            new CustomerCustomerRelationshipMapping().Register();
            new InnerCustomerMapping().Register();
            new OuterCustomerMapping().Register();
            new ProductMapping().Register();
            new TransactionMapping().Register();
            new CustomerCustomerEdgeMapping().Register();

            MappingRegistry.BuildDottedAliases();
        }
    }
}