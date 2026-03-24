using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;

namespace Domain.Shared.Mapping;
using DataEntity = Database.Entity;

public class CustomerCustomerRelationshipMapping : IMappingRegistration
{
    public void RegisterNodeMap(Dictionary<string, NodeMap> mappings)
    {
        var ccr = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Banking)
        };

        ccr.IsEntity = true;
        ccr.IsModel = true;

        ccr.Children.Add(nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer));
        ccr.Children.Add(nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer));

        ccr.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship),
            nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)));

        ccr.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(DataEntity.CustomerCustomerRelationship.Id),
            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
            DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id)
        });

        ccr.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey),
            DestinationEntity = nameof(DataEntity.Customer),
            DestinationName = nameof(DataEntity.Customer.CustomerKey)
        });
            
        ccr.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(DataEntity.Customer.CustomerKey),
            DestinationEntity = nameof(DataEntity.Customer),
            DestinationName = nameof(DataEntity.Customer.CustomerKey)
        });
        
        ccr.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey),
            DestinationEntity = nameof(DataEntity.Customer),
            DestinationName = nameof(DataEntity.Customer.CustomerKey)
        });
        
        

        ccr.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipKey),
            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
            DestinationName = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)
        });

        ccr.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipType),
            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
            DestinationName = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipType)
        });

        mappings.TryAdd(nameof(CustomerCustomerRelationship), MappingRegistry.Register(typeof(CustomerCustomerRelationship),
            typeof(DataEntity.CustomerCustomerRelationship), ccr));
    }

    public void Register(Dictionary<string, NodeMap> mappings)
    {
        RegisterNodeMap(mappings);
    }
}