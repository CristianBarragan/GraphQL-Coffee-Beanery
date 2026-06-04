using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using DataEntity = Database.Entity;

namespace Domain.Shared.Mapping;

public class CustomerCustomerRelationshipMappingSet : IMappingSet<CustomerMappingType, Domain.Model.Model>
{
    public void Register(CustomerMappingType type, Domain.Model.Model model)
    {
        new CustomerCustomerRelationshipMapping(type.ToString(), model.ToString()).Register();
    }
}

public class CustomerCustomerRelationshipMapping
    : BaseMappingRegistration<CustomerCustomerRelationship, DataEntity.CustomerCustomerRelationship>
{
    public CustomerCustomerRelationshipMapping(string alias, string model) : base(alias, model)
    {
    }

    protected override NodeMap BuildMap()
    {
        var map = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Banking)
        };
        
        map.EntityChildren.AddRange(new LinkKey[]
            {
                new(){
                    AliasFrom    = A(nameof(DataEntity.CustomerCustomerRelationship)),
                    From       = nameof(DataEntity.CustomerCustomerRelationship),
                    FromColumn = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerId),
                    AliasTo    = A(nameof(DataEntity.Customer)),
                    To         = nameof(DataEntity.Customer),
                    ToColumn   = nameof(DataEntity.Customer.Id)
                },
                new(){
                    AliasFrom    = A(nameof(DataEntity.CustomerCustomerRelationship)),
                    From       = nameof(DataEntity.CustomerCustomerRelationship),
                    FromColumn = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerId),
                    AliasTo    = A(nameof(DataEntity.Customer)),
                    To         = nameof(DataEntity.Customer),
                    ToColumn   = nameof(DataEntity.Customer.Id)
                }
            }
        );
        
        map.ModelParents.AddRange(new []
        {
            new LinkKey
            {
                AliasFrom    = A(nameof(CustomerCustomerRelationship)), 
                From       = nameof(CustomerCustomerRelationship),
                FromColumn = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipKey),
                AliasTo    = A(nameof(CustomerCustomerEdge)),
                To         = nameof(CustomerCustomerEdge),
                ToColumn   = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey)
            },
            new LinkKey
            {
                AliasFrom    = A(nameof(CustomerCustomerRelationship)),
                From       = nameof(CustomerCustomerRelationship),
                FromColumn = nameof(CustomerCustomerRelationship.InnerCustomerKey),
                AliasTo    = A(nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer)),
                To         = nameof(Customer),
                ToColumn   = nameof(Customer.CustomerKey),
            },
            new LinkKey
            {
                AliasFrom    = A(nameof(CustomerBankingRelationship)),
                From       = nameof(CustomerBankingRelationship),
                FromColumn = A(nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer)),
                AliasTo    = A(nameof(DataEntity.Customer)),
                To         = nameof(Customer),
                ToColumn   = nameof(Customer.CustomerKey),
            }
        });

        map.ModelToEntityLinks.AddRange(new[]
        {
            new LinkKey { AliasFrom    = A(nameof(CustomerCustomerRelationship)), From = nameof(CustomerCustomerRelationship), FromColumn = nameof(CustomerCustomerRelationship.InnerCustomerKey), 
                AliasTo    = A(nameof(CustomerCustomerRelationship.InnerCustomer), nameof(DataEntity.Customer)),
                To         = nameof(DataEntity.Customer), 
                ToColumn = nameof(DataEntity.Customer.CustomerKey) },
            new LinkKey { AliasFrom    = A(nameof(CustomerCustomerRelationship)), From = nameof(CustomerCustomerRelationship), FromColumn = nameof(CustomerCustomerRelationship.OuterCustomerKey), 
                AliasTo    = A(nameof(CustomerCustomerRelationship.OuterCustomer), nameof(DataEntity.Customer)),
                To         = nameof(DataEntity.Customer),
                ToColumn = nameof(DataEntity.Customer.CustomerKey) }
        });

        map.UpsertKeys.Add(new UpsertKey(
            nameof(DataEntity.CustomerCustomerRelationship),
            nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)
        ));

        map.FieldMaps.AddRange(new[]
        {
            new FieldMap { SourceAlias = A(nameof(CustomerCustomerRelationship)), DestinationAlias = A(nameof(DataEntity.CustomerCustomerRelationship)), SourceName = nameof(DataEntity.CustomerCustomerRelationship.Id),                              DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id) },
            new FieldMap { SourceAlias = A(nameof(CustomerCustomerRelationship)), DestinationAlias = A(nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer), nameof(DataEntity.Customer)), SourceName = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey),                DestinationEntity = nameof(DataEntity.Customer), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey) },
            new FieldMap { SourceAlias = A(nameof(CustomerCustomerRelationship)), DestinationAlias = A(nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer), nameof(DataEntity.Customer)), SourceName = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey),                DestinationEntity = nameof(DataEntity.Customer), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey) },
            new FieldMap { SourceAlias = A(nameof(CustomerCustomerRelationship)), DestinationAlias = A(nameof(DataEntity.CustomerCustomerRelationship)), SourceName = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipKey),            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey) },
            new FieldMap { SourceAlias = A(nameof(CustomerCustomerRelationship)), DestinationAlias = A(nameof(DataEntity.CustomerCustomerRelationship)), SourceName = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipType),           DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipType) }
        });

        return map;
    }
}