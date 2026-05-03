using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using DataEntity = Database.Entity;

namespace Domain.Shared.Mapping;

public class CustomerCustomerEdgeMapping
    : BaseMappingRegistration<CustomerCustomerEdge, DataEntity.CustomerCustomerRelationship>
{
    protected override string Alias    => nameof(CustomerCustomerEdge);
    protected override bool   IsEntity => false;

    protected override NodeMap BuildMap()
    {
        var map = new NodeMap();

        map.ModelChildren.AddRange(new[]
        {
            new LinkKey { From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.OuterCustomerKey), To = nameof(Customer), ToColumn = nameof(Customer.CustomerKey) },
            new LinkKey { From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey), To = nameof(Customer), ToColumn = nameof(Customer.CustomerKey) }
        });

        map.ModelToEntityLinks.AddRange(new[]
        {
            new LinkKey { From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey), To = nameof(DataEntity.CustomerCustomerRelationship), ToColumn = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey) },
            new LinkKey { From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey),               To = nameof(DataEntity.Customer),                    ToColumn = nameof(DataEntity.Customer.CustomerKey) },
            new LinkKey { From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.OuterCustomerKey),               To = nameof(DataEntity.Customer),                    ToColumn = nameof(DataEntity.Customer.CustomerKey) }
        });

        map.UpsertKeys.AddRange(new[]
        {
            new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship), nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey)),
            new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship), nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey)),
            new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship), nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey))
        });

        map.FieldMaps.AddRange(new[]
        {
            new FieldMap { SourceName = nameof(CustomerCustomerEdge.InnerCustomerKey),                DestinationEntity = nameof(CustomerCustomerEdge.InnerCustomer),                DestinationName = nameof(CustomerCustomerEdge.InnerCustomerKey) },
            new FieldMap { SourceName = nameof(CustomerCustomerEdge.OuterCustomerKey),                DestinationEntity = nameof(CustomerCustomerEdge.OuterCustomer),                DestinationName = nameof(CustomerCustomerEdge.OuterCustomerKey) },
            new FieldMap { SourceName = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey), DestinationEntity = nameof(CustomerCustomerEdge.CustomerCustomerRelationship), DestinationName = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey) }
        });

        return map;
    }
}