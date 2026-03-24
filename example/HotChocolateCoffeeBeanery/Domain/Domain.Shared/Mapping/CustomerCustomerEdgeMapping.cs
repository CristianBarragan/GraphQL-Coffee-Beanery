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
            Schema = nameof(CustomerCustomerEdge),
            IsModel = true,
            IsEntity = true
        };
        
        customerCustomerEdge.Children.Add(nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer));
        customerCustomerEdge.Children.Add(nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer));
        
        customerCustomerEdge.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.CustomerBankingRelationship),
            nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer)));
        customerCustomerEdge.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.CustomerBankingRelationship),
            nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerId)));
        customerCustomerEdge.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.CustomerBankingRelationship),
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