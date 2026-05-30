using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using DataEntity = Database.Entity;

namespace Domain.Shared.Mapping;

public class CustomerCustomerEdgeMappingSet : IMappingSet<CustomerMappingType>
{
    public void Register(CustomerMappingType type)
    {
        new CustomerCustomerEdgeMapping(type.ToString()).Register();
    }
}

public class CustomerCustomerEdgeMapping
    : BaseModelMappingRegistration<CustomerCustomerEdge>
{
    public CustomerCustomerEdgeMapping(string alias) : base(alias)
    {
    }

    protected override NodeMap BuildMap()
    {
        var map = new NodeMap();

        map.ModelChildren.AddRange(new[]
        {
            new LinkKey { AliasFrom    = A(nameof(CustomerCustomerEdge)), From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.OuterCustomerKey), AliasTo = A(nameof(Customer)), To = nameof(Customer), ToColumn = nameof(Customer.CustomerKey) },
            new LinkKey { AliasFrom    = A(nameof(CustomerCustomerEdge)), From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey), AliasTo = A(nameof(Customer)), To = nameof(Customer), ToColumn = nameof(Customer.CustomerKey) }
        });

        map.ModelToEntityLinks.AddRange(new[]
        {
            new LinkKey { AliasFrom    = A(nameof(CustomerCustomerEdge)), From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey), AliasTo = A(nameof(DataEntity.CustomerCustomerRelationship)), To = nameof(DataEntity.CustomerCustomerRelationship), ToColumn = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey) },
            new LinkKey { AliasFrom    = A(nameof(CustomerCustomerEdge)), From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey),               AliasTo = A(nameof(DataEntity.CustomerCustomerRelationship)), To = nameof(DataEntity.CustomerCustomerRelationship),                    ToColumn = nameof(DataEntity.Customer.CustomerKey) },
            new LinkKey { AliasFrom    = A(nameof(CustomerCustomerEdge)), From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.OuterCustomerKey),               AliasTo = A(nameof(DataEntity.CustomerCustomerRelationship)), To = nameof(DataEntity.CustomerCustomerRelationship),                    ToColumn = nameof(DataEntity.Customer.CustomerKey) }
        });
        
        map.UpsertKeys.AddRange(new[]
        {
            new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship), nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey)),
            new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship), nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey)),
            new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship), nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey))
        });
            
        map.FieldMaps.AddRange(new[]
        {
            new FieldMap { SourceName = nameof(CustomerCustomerEdge.InnerCustomerKey),                DestinationEntity = nameof(DataEntity.Customer), DestinationName = nameof(CustomerCustomerEdge.InnerCustomerKey) },
            new FieldMap { SourceName = nameof(CustomerCustomerEdge.OuterCustomerKey),                DestinationEntity = nameof(DataEntity.Customer), DestinationName = nameof(CustomerCustomerEdge.OuterCustomerKey) },
            new FieldMap { SourceName = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey), DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship), DestinationName = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey) }
        });

        return map;
    }
}