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
            Schema = nameof(DataEntity.Schema.Banking),

            // To must match the key used in mappings.TryAdd() in each child mapping:
            //   InnerCustomerMapping  → mappings.TryAdd("InnerCustomer", ...)
            //   OuterCustomerMapping  → mappings.TryAdd("OuterCustomer", ...)
            EntityChildren = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From       = nameof(DataEntity.CustomerCustomerRelationship),
                    FromColumn = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerId),
                    To         = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer), // "InnerCustomer"
                    ToColumn   = nameof(DataEntity.Customer.Id)
                },
                new LinkKey()
                {
                    From       = nameof(DataEntity.CustomerCustomerRelationship),
                    FromColumn = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerId),
                    To         = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer), // "OuterCustomer"
                    ToColumn   = nameof(DataEntity.Customer.Id)
                }
            },
            ModelParents = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From       = nameof(CustomerCustomerRelationship),
                    FromColumn = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipKey),
                    To         = nameof(CustomerCustomerEdge),
                    ToColumn   = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey)
                }
            },
            ModelToEntityLinks =
            {
                new LinkKey()
                {
                    From       = nameof(CustomerCustomerEdge.InnerCustomer),
                    FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey),
                    To         = nameof(DataEntity.Customer),
                    ToColumn   = nameof(DataEntity.Customer.CustomerKey)
                },
                new LinkKey()
                {
                    From       = nameof(CustomerCustomerEdge.OuterCustomer),
                    FromColumn = nameof(CustomerCustomerEdge.OuterCustomerKey),
                    To         = nameof(DataEntity.Customer),
                    ToColumn   = nameof(DataEntity.Customer.CustomerKey)
                }
            }
        };

        ccr.IsEntity = true;
        ccr.IsModel  = true;

        ccr.UpsertKeys.Add(new UpsertKey(
            nameof(DataEntity.CustomerCustomerRelationship),
            nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)));

        ccr.FieldMaps.Add(new FieldMap
        {
            SourceName        = nameof(DataEntity.CustomerCustomerRelationship.Id),
            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
            DestinationName   = nameof(DataEntity.CustomerCustomerRelationship.Id)
        });

        ccr.FieldMaps.Add(new FieldMap
        {
            SourceName        = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey),
            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
            DestinationName   = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey)
        });

        ccr.FieldMaps.Add(new FieldMap
        {
            SourceName        = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey),
            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
            DestinationName   = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey)
        });

        ccr.FieldMaps.Add(new FieldMap
        {
            SourceName        = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipKey),
            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
            DestinationName   = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)
        });

        ccr.FieldMaps.Add(new FieldMap
        {
            SourceName        = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipType),
            DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
            DestinationName   = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipType)
        });

        mappings.TryAdd(nameof(CustomerCustomerRelationship),
            MappingRegistry.Register(
                typeof(CustomerCustomerRelationship),
                typeof(DataEntity.CustomerCustomerRelationship),
                ccr));
    }

    public void Register(Dictionary<string, NodeMap> mappings)
    {
        RegisterNodeMap(mappings);
    }
}