using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using DataEntity = Database.Entity;
using DataGraph = Database.Graph;

namespace Domain.Shared.Mapping;

public class CustomerCustomerRelationshipEdgeMapping
{
    public void RegisterNodeMap(Dictionary<string, NodeMap> mappings)
    {
        var customerCustomerRelationshipEdge = new NodeMap
        {
            Schema = nameof(DataGraph.CustomerCustomerRelationshipEdge),
            IsGraph = true,
            IsEntity = true,
            IsModel = true,
            EntityChildren = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(DataGraph.CustomerCustomerRelationshipEdge),
                    FromColumn = nameof(DataGraph.CustomerCustomerRelationshipEdge.InnerCustomerId),
                    To = nameof(DataEntity.Customer),
                    ToColumn = nameof(DataEntity.Customer.Id)
                },
                new LinkKey()
                {
                    From = nameof(DataGraph.CustomerCustomerRelationshipEdge),
                    FromColumn = nameof(DataGraph.CustomerCustomerRelationshipEdge.OuterCustomerId),
                    To = nameof(DataEntity.Customer),
                    ToColumn = nameof(DataEntity.Customer.Id)
                },
                new LinkKey()
                {
                    From = nameof(DataGraph.CustomerCustomerRelationshipEdge),
                    FromColumn = nameof(DataGraph.CustomerCustomerRelationshipEdge.Id),
                    To = nameof(DataEntity.CustomerCustomerRelationship),
                    ToColumn = nameof(DataEntity.CustomerCustomerRelationship.Id)
                }
            },
            ModelChildren = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(CustomerCustomerRelationshipEdge),
                    FromColumn = nameof(CustomerCustomerRelationshipEdge.CustomerCustomerRelationshipKey),
                    To = nameof(CustomerCustomerRelationship),
                    ToColumn = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipKey)
                },
                new LinkKey()
                {
                    From = nameof(CustomerCustomerRelationshipEdge),
                    FromColumn = nameof(CustomerCustomerEdge.OuterCustomerKey),
                    To = nameof(Customer),
                    ToColumn = nameof(Customer.CustomerKey)
                },
                new LinkKey()
                {
                    From = nameof(CustomerCustomerRelationshipEdge),
                    FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey),
                    To = nameof(Customer),
                    ToColumn = nameof(Customer.CustomerKey)
                }
            },
            ModelToEntityLinks =
            {
                new LinkKey()
                {
                    From = nameof(CustomerCustomerRelationshipEdge),
                    FromColumn = nameof(CustomerCustomerRelationshipEdge.CustomerCustomerRelationshipKey),
                    To = nameof(DataEntity.CustomerCustomerRelationship),
                    ToColumn = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)
                },
                new LinkKey()
                {
                    From = nameof(CustomerCustomerRelationshipEdge),
                    FromColumn = nameof(CustomerCustomerRelationshipEdge.InnerCustomerKey),
                    To = nameof(DataEntity.Customer),
                    ToColumn = nameof(DataEntity.Customer.CustomerKey)
                },
                new LinkKey()
                {
                    From = nameof(CustomerCustomerRelationshipEdge),
                    FromColumn = nameof(CustomerCustomerRelationshipEdge.OuterCustomerKey),
                    To = nameof(DataEntity.Customer),
                    ToColumn = nameof(DataEntity.Customer.CustomerKey)
                }
            }
        };
        
        // customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
        // {
        //     SourceName = nameof(CustomerCustomerEdge.Clause),
        //     DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship)
        //     // ,
        //     // DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id)
        // });
        //
        // customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
        // {
        //     SourceName = nameof(CustomerCustomerEdge.LevelDepth),
        //     DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship)
        //     // ,
        //     // DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id)
        // });
        //
        // customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
        // {
        //     SourceName = nameof(CustomerCustomerEdge.LevelDirection),
        //     DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship)
        //     // ,
        //     // DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id)
        // });
        
        customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(DataGraph.CustomerCustomerRelationshipEdge.Id),
            DestinationEntity = nameof(DataGraph.CustomerCustomerRelationshipEdge),
            DestinationName = nameof(DataGraph.CustomerCustomerRelationshipEdge.Id)
        });
        
        customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(DataGraph.CustomerCustomerRelationshipEdge.InnerCustomerId),
            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
            DestinationName = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerId)
        });
        
        customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(DataGraph.CustomerCustomerRelationshipEdge.OuterCustomerId),
            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
            DestinationName = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerId)
        });
        
        customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(CustomerCustomerEdge.OuterCustomer),
            DestinationEntity = nameof(DataEntity.Customer),
            DestinationName = nameof(DataEntity.Customer.CustomerKey)
        });
        
        mappings.TryAdd(nameof(DataGraph.CustomerCustomerRelationshipEdge), MappingRegistry.Register(typeof(CustomerCustomerEdge), typeof(DataGraph.CustomerCustomerRelationshipEdge),
            customerCustomerRelationshipEdge));
    }

    public void Register(Dictionary<string, NodeMap> mappings)
    {
        RegisterNodeMap(mappings);
    }
}