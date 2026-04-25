using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using DataEntity = Database.Entity;
using DataGraph = Database.Graph;

namespace Domain.Shared.Mapping;

public class CustomerCustomerEdgeMapping
{
    public void RegisterNodeMap(Dictionary<string, NodeMap> mappings)
    {
        var customerCustomerEdge = new NodeMap
        {
            ModelChildren = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(CustomerCustomerEdge),
                    FromColumn = nameof(CustomerCustomerEdge.OuterCustomerKey),
                    To = nameof(Customer),
                    ToColumn = nameof(Customer.CustomerKey)
                },
                new LinkKey()
                {
                    From = nameof(CustomerCustomerEdge),
                    FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey),
                    To = nameof(Customer),
                    ToColumn = nameof(Customer.CustomerKey)
                }
            },
            ModelToEntityLinks =
            {
                new LinkKey()
                {
                    From = nameof(CustomerCustomerEdge),
                    FromColumn = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey),
                    To = nameof(DataEntity.CustomerCustomerRelationship),
                    ToColumn = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)
                },
                new LinkKey()
                {
                    From = nameof(CustomerCustomerEdge),
                    FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey),
                    To = nameof(DataEntity.Customer),
                    ToColumn = nameof(DataEntity.Customer.CustomerKey)
                },
                new LinkKey()
                {
                    From = nameof(CustomerCustomerEdge),
                    FromColumn = nameof(CustomerCustomerEdge.OuterCustomerKey),
                    To = nameof(DataEntity.Customer),
                    ToColumn = nameof(DataEntity.Customer.CustomerKey)
                }
            }
        };
        
        customerCustomerEdge.IsModel = true;
        
        customerCustomerEdge.UpsertKeys.Add(
            new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship),
                nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey)));

        customerCustomerEdge.UpsertKeys.Add(
            new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship),
                nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey)));

        customerCustomerEdge.UpsertKeys.Add(
            new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship),
                nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)));
        
        customerCustomerEdge.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(CustomerCustomerEdge.InnerCustomerKey),
            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
            DestinationName = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey)
        });
        
        customerCustomerEdge.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(CustomerCustomerEdge.OuterCustomerKey),
            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
            DestinationName = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey)
        });
        
        customerCustomerEdge.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey),
            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
            DestinationName = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)
        });
    
        mappings.TryAdd(nameof(CustomerCustomerEdge), MappingRegistry.Register(typeof(CustomerCustomerEdge), typeof(DataEntity.CustomerCustomerRelationship),
            customerCustomerEdge));
    }

    public void Register(Dictionary<string, NodeMap> mappings)
    {
        RegisterNodeMap(mappings);
    }
}