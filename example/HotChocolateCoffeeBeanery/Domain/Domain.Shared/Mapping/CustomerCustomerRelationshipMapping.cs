using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using DataEntity = Database.Entity;

namespace Domain.Shared.Mapping;

public class CustomerCustomerRelationshipMappingSet : IMappingSet<CustomerMappingType>
{
    public void Register(CustomerMappingType type)
    {
        new CustomerCustomerRelationshipMapping(type.ToString()).Register();
    }
}

public class CustomerCustomerRelationshipMapping
    : BaseMappingRegistration<CustomerCustomerRelationship, DataEntity.CustomerCustomerRelationship>
{
    public CustomerCustomerRelationshipMapping(string alias) : base(alias)
    {
    }

    protected override NodeMap BuildMap()
    {
        var map = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Banking)
        };

        map.EntityChildren.AddRange(new[]
        {
            new LinkKey { From = nameof(DataEntity.CustomerCustomerRelationship), FromColumn = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerId), To = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer), ToColumn = nameof(DataEntity.Customer.Id) },
            new LinkKey { From = nameof(DataEntity.CustomerCustomerRelationship), FromColumn = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerId), To = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer), ToColumn = nameof(DataEntity.Customer.Id) }
        });

        map.ModelParents.Add(new LinkKey
        {
            From       = nameof(CustomerCustomerRelationship),
            FromColumn = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipKey),
            To         = nameof(CustomerCustomerEdge),
            ToColumn   = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey)
        });

        map.ModelToEntityLinks.AddRange(new[]
        {
            new LinkKey { From = nameof(CustomerCustomerEdge.InnerCustomer), FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey), To = nameof(DataEntity.Customer), ToColumn = nameof(DataEntity.Customer.CustomerKey) },
            new LinkKey { From = nameof(CustomerCustomerEdge.OuterCustomer), FromColumn = nameof(CustomerCustomerEdge.OuterCustomerKey), To = nameof(DataEntity.Customer), ToColumn = nameof(DataEntity.Customer.CustomerKey) }
        });

        map.UpsertKeys.Add(new UpsertKey(
            nameof(DataEntity.CustomerCustomerRelationship),
            nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)
        ));

        map.FieldMaps.AddRange(new[]
        {
            new FieldMap { SourceName = nameof(DataEntity.CustomerCustomerRelationship.Id),                              DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id) },
            new FieldMap { SourceName = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey),                DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey) },
            new FieldMap { SourceName = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey),                DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey) },
            new FieldMap { SourceName = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipKey),            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey) },
            new FieldMap { SourceName = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipType),           DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipType) }
        });

        return map;
    }
}