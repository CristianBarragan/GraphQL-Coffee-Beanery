
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace Domain.Shared.Mapping
{
    public class ModelMappingRegistration : IMappingRegistration
    {
        public void Register(Dictionary<string, NodeMap> mappings)
        {
            new AccountMapping().Register(mappings);
            new ContactPointMapping().Register(mappings);
            new ContractMapping().Register(mappings);
            new CustomerBankingRelationshipMapping().Register(mappings);
            new CustomerCustomerRelationshipEdgeMapping().Register(mappings);
            new CustomerCustomerRelationshipMapping().Register(mappings);
            new CustomerMapping().Register(mappings);
            new ProductMapping().Register(mappings);
            new TransactionMapping().Register(mappings);
        }
    }
}