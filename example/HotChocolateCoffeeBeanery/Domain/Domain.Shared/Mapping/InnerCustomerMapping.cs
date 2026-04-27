using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;

namespace Domain.Shared.Mapping;
using DataEntity = Database.Entity;

public class InnerCustomerMapping : IMappingRegistration
{
    public void RegisterNodeMap(Dictionary<string, NodeMap> mappings)
    {
        var cust = new NodeMap
        {
            Alias = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer),
            Schema = nameof(DataEntity.Schema.Banking),
            EntityChildren = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(DataEntity.Customer),
                    FromColumn = nameof(DataEntity.Customer.Id),
                    To = nameof(DataEntity.ContactPoint),
                    ToColumn = nameof(DataEntity.ContactPoint.CustomerId)
                },
                new LinkKey()
                {
                    From = nameof(DataEntity.Customer),
                    FromColumn = nameof(DataEntity.Customer.Id),
                    To = nameof(DataEntity.CustomerBankingRelationship),
                    ToColumn = nameof(DataEntity.CustomerBankingRelationship.CustomerId)
                }
            },
            EntityParents = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(DataEntity.Customer),
                    FromColumn = nameof(DataEntity.Customer.Id),
                    To = nameof(DataEntity.CustomerCustomerRelationship),
                    ToColumn = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerId)
                },
                new LinkKey()
                {
                    From = nameof(DataEntity.Customer),
                    FromColumn = nameof(DataEntity.Customer.Id),
                    To = nameof(DataEntity.CustomerCustomerRelationship),
                    ToColumn = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerId)
                }
            },
            ModelToEntityLinks =
            {
                new LinkKey()
                {
                    From = nameof(CustomerCustomerEdge.InnerCustomer),
                    FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey),
                    To = nameof(DataEntity.Customer),
                    ToColumn = nameof(DataEntity.Customer.CustomerKey)
                },
                new LinkKey()
                {
                    From = nameof(CustomerCustomerEdge.OuterCustomer),
                    FromColumn = nameof(CustomerCustomerEdge.OuterCustomerKey),
                    To = nameof(DataEntity.Customer),
                    ToColumn = nameof(DataEntity.Customer.CustomerKey)
                }
            }
        };

        cust.IsEntity = true;
        cust.IsModel = true;

        cust.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.Customer), nameof(DataEntity.Customer.CustomerKey)));

        cust.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(DataEntity.Customer.Id),
            DestinationEntity = nameof(DataEntity.Customer),
            DestinationName = nameof(DataEntity.Customer.Id)
        });

        cust.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Customer.CustomerKey),
            DestinationEntity = nameof(DataEntity.Customer),
            DestinationName = nameof(DataEntity.Customer.CustomerKey)
        });
        
        cust.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Customer.CustomerKey),
            DestinationEntity = nameof(DataEntity.Customer),
            DestinationName = nameof(DataEntity.Customer.CustomerType)
        });

        cust.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Customer.FirstNaming),
            DestinationEntity = nameof(DataEntity.Customer),
            DestinationName = nameof(DataEntity.Customer.FirstName)
        });

        cust.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Customer.LastNaming),
            DestinationEntity = nameof(DataEntity.Customer),
            DestinationName = nameof(DataEntity.Customer.LastName)
        });

        cust.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Customer.FullNaming),
            DestinationEntity = nameof(DataEntity.Customer),
            DestinationName = nameof(DataEntity.Customer.FullName)
        });

        // Enum mapping for CustomerType
        var custEnums = EnumMapFactory.Create(
            new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
            {
                { $"{nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer)}~{nameof(DataEntity.Customer)}~{DataEntity.CustomerType.Person}", (CustomerType.Person.ToString(), (int)CustomerType.Person) },
                { $"{nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer)}~{nameof(DataEntity.Customer)}~{DataEntity.CustomerType.Organisation}", (CustomerType.Organisation.ToString(), (int)CustomerType.Organisation) }
            },
            new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
            {
                { $"{nameof(DataEntity.Customer)}~{nameof(Customer)}~{CustomerType.Person}", (DataEntity.CustomerType.Person.ToString(), (int)CustomerType.Person) },
                { $"{nameof(DataEntity.Customer)}~{nameof(Customer)}~{CustomerType.Organisation}", (DataEntity.CustomerType.Organisation.ToString(), (int)CustomerType.Organisation) }
            });

        cust.FromEnum = custEnums.from;
        cust.ToEnum = custEnums.to;

        mappings.TryAdd(nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer), MappingRegistry.Register(typeof(Customer), typeof(DataEntity.Customer), cust, nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer)));
    }

    public void Register(Dictionary<string, NodeMap> mappings)
    {
        RegisterNodeMap(mappings);
    }
}