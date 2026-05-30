using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using DataEntity = Database.Entity;

namespace Domain.Shared.Mapping;

public class CustomerBankingRelationshipMappingSet : IMappingSet<CustomerMappingType>
{
    public void Register(CustomerMappingType type)
    {
        new CustomerBankingRelationshipMapping(type.ToString()).Register();
    }
}

public class CustomerBankingRelationshipMapping
    : BaseMappingRegistration<CustomerBankingRelationship, DataEntity.CustomerBankingRelationship>
{
    public CustomerBankingRelationshipMapping(string alias) : base(alias)
    {
    }

    protected override NodeMap BuildMap()
    {
        var map = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Banking)
        };

        map.EntityParents.Add(new LinkKey
        {
            AliasFrom    = A(nameof(DataEntity.CustomerBankingRelationship)),
            From       = nameof(DataEntity.CustomerBankingRelationship),
            FromColumn = nameof(DataEntity.CustomerBankingRelationship.CustomerId),
            AliasTo    = A(nameof(DataEntity.Customer)),
            To         = nameof(DataEntity.Customer),
            ToColumn   = nameof(DataEntity.Customer.Id)
        });

        map.EntityChildren.Add(new LinkKey
        {
            AliasFrom    = A(nameof(DataEntity.CustomerBankingRelationship)),
            From       = nameof(DataEntity.CustomerBankingRelationship),
            FromColumn = nameof(DataEntity.CustomerBankingRelationship.Id),
            AliasTo    = A(nameof(DataEntity.Contract)),
            To         = nameof(DataEntity.Contract),
            ToColumn   = nameof(DataEntity.Contract.CustomerBankingRelationshipId)
        });

        map.ModelParents.AddRange(new LinkKey[]
            {
                new(){
                    AliasFrom    = A(nameof(DataEntity.CustomerBankingRelationship)),
                    From       = nameof(DataEntity.CustomerBankingRelationship),
                    FromColumn = nameof(DataEntity.CustomerBankingRelationship.CustomerId),
                    AliasTo    = A(nameof(DataEntity.Customer)),
                    To         = nameof(DataEntity.Customer),
                    ToColumn   = nameof(DataEntity.Customer.Id)
                }
            }
        );

        map.ModelChildren.Add(new LinkKey
        {
            AliasFrom    = A(nameof(CustomerBankingRelationship)),
            From       = nameof(CustomerBankingRelationship),
            FromColumn = nameof(CustomerBankingRelationship.CustomerBankingRelationshipKey),
            AliasTo    = A(nameof(DataEntity.Contract)),
            To         = nameof(DataEntity.Contract),
            ToColumn   = nameof(Contract.CustomerBankingRelationshipKey)
        });

        map.ModelToEntityLinks.Add(new LinkKey
        {
            AliasFrom    = A(nameof(CustomerBankingRelationship)),
            From       = nameof(CustomerBankingRelationship),
            FromColumn = nameof(CustomerBankingRelationship.CustomerBankingRelationshipKey),
            AliasTo    = A(nameof(DataEntity.CustomerBankingRelationship)),
            To         = nameof(DataEntity.CustomerBankingRelationship),
            ToColumn   = nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey)
        });

        map.UpsertKeys.Add(new UpsertKey(
            nameof(DataEntity.CustomerBankingRelationship),
            nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey)
        ));

        map.FieldMaps.AddRange(new[]
        {
            new FieldMap { SourceName = nameof(DataEntity.CustomerBankingRelationship.Id),                       DestinationEntity = nameof(DataEntity.CustomerBankingRelationship), DestinationName = nameof(DataEntity.CustomerBankingRelationship.Id) },
            new FieldMap { SourceName = nameof(CustomerBankingRelationship.CustomerBankingRelationshipKey),       DestinationEntity = nameof(DataEntity.CustomerBankingRelationship), DestinationName = nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey) },
            new FieldMap { SourceName = nameof(CustomerBankingRelationship.CustomerKey),                         DestinationEntity = nameof(DataEntity.CustomerBankingRelationship), DestinationName = nameof(DataEntity.CustomerBankingRelationship.CustomerKey) }
        });

        return map;
    }
}