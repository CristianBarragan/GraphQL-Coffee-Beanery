using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using DataEntity = Database.Entity;

namespace Domain.Shared.Mapping;

public class CustomerCustomerEdgeMappingSet : IMappingSet<CustomerMappingType, Domain.Model.Model>
{
    public void Register(CustomerMappingType type, Domain.Model.Model model)
    {
        new CustomerCustomerEdgeMapping(type.ToString(), model.ToString()).Register();
    }
}

public class CustomerCustomerEdgeMapping
    : BaseModelMappingRegistration<CustomerCustomerEdge>
{
    public CustomerCustomerEdgeMapping(string alias, string model) : base(alias, model)
    {
    }

    protected override NodeMap BuildMap()
    {
        var map = new NodeMap();

        map.ModelChildren.AddRange(new[]
        {
            new LinkKey { AliasFrom    = A(nameof(CustomerCustomerEdge)), From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey), AliasTo = A(nameof(CustomerCustomerRelationship)), To = nameof(CustomerCustomerRelationship), ToColumn = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipKey) },
            new LinkKey { AliasFrom    = A(nameof(CustomerCustomerEdge)), From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey), AliasTo = A(nameof(Customer)), To = nameof(CustomerCustomerEdge.InnerCustomer), ToColumn = nameof(CustomerCustomerRelationship.InnerCustomerKey) },
            new LinkKey { AliasFrom    = A(nameof(CustomerCustomerEdge)), From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.OuterCustomerKey), AliasTo = A(nameof(Customer)), To = nameof(CustomerCustomerEdge.OuterCustomer), ToColumn = nameof(CustomerCustomerRelationship.OuterCustomerKey) }
        });
        
        map.ModelParents.Add(new LinkKey
        {
            AliasFrom  = A(nameof(CustomerCustomerEdge)),
            From       = nameof(CustomerCustomerEdge),
            AliasTo    = A(nameof(Wrapper)),
            To         = nameof(Wrapper)
        });
        
        map.ModelToEntityLinks.AddRange(new[]
        {
            new LinkKey { AliasFrom    = A(nameof(CustomerCustomerEdge)), From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.InnerCustomer), AliasTo = A(nameof(DataEntity.CustomerCustomerRelationship)), To = nameof(DataEntity.CustomerCustomerRelationship), ToColumn = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey) },
            new LinkKey { AliasFrom    = A(nameof(CustomerCustomerEdge)), From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.OuterCustomer), AliasTo = A(nameof(DataEntity.CustomerCustomerRelationship)), To = nameof(DataEntity.CustomerCustomerRelationship), ToColumn = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey) },
            new LinkKey { AliasFrom    = A(nameof(CustomerCustomerEdge)), From = nameof(CustomerCustomerEdge), FromColumn = nameof(CustomerCustomerEdge.CustomerCustomerRelationship), AliasTo = A(nameof(DataEntity.CustomerCustomerRelationship)), To = nameof(DataEntity.CustomerCustomerRelationship), ToColumn = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey) },
            new LinkKey
            {
                AliasFrom    = A(nameof(CustomerCustomerEdge)),
                From       = nameof(CustomerCustomerEdge),
                FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey),
                AliasTo    = A(nameof(Customer)),
                To         = nameof(Customer),
                ToColumn   = nameof(Customer.CustomerKey)
            },
            new LinkKey
            {
                AliasFrom    = A(nameof(CustomerCustomerEdge)),
                From       = nameof(CustomerCustomerEdge),
                FromColumn = A(nameof(CustomerCustomerEdge.OuterCustomerKey)),
                AliasTo    = A(nameof(Customer)),
                To         = nameof(Customer),
                ToColumn   = nameof(Customer.CustomerKey)
            }
        });
        
        map.UpsertKeys.AddRange(new[]
        {
            new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship), nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey)),
            new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship), nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey)),
            new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship), nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey))
        });
            
        map.FieldMaps.AddRange(new[]
        {
            new FieldMap { SourceAlias = A(nameof(CustomerCustomerEdge)), DestinationAlias = A(nameof(DataEntity.CustomerCustomerRelationship)), SourceName = nameof(CustomerCustomerEdge.InnerCustomerKey),                DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey) },
            new FieldMap { SourceAlias = A(nameof(CustomerCustomerEdge)), DestinationAlias = A(nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer)), SourceName = nameof(CustomerCustomerEdge.InnerCustomer),                DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer) },
            new FieldMap { SourceAlias = A(nameof(CustomerCustomerEdge)), DestinationAlias = A(nameof(DataEntity.CustomerCustomerRelationship)), SourceName = nameof(CustomerCustomerEdge.OuterCustomerKey),                DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey) },
            new FieldMap { SourceAlias = A(nameof(CustomerCustomerEdge)), DestinationAlias = A(nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer)), SourceName = nameof(CustomerCustomerEdge.OuterCustomer),                DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer) },
            new FieldMap { SourceAlias = A(nameof(CustomerCustomerEdge)), DestinationAlias = A(nameof(DataEntity.CustomerCustomerRelationship)), SourceName = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey), DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship), DestinationName = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey) }
        });

        return map;
    }
}