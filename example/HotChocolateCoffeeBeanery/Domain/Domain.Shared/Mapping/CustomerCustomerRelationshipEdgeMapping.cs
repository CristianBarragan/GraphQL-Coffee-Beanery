using CoffeeBeanery.GraphQL.Core.Mapping;
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
            IsModel = true
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
        
        customerCustomerRelationshipEdge.Children.Add(nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer));
        customerCustomerRelationshipEdge.Children.Add(nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer));
        
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