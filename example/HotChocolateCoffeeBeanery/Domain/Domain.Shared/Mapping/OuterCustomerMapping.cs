using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;

namespace Domain.Shared.Mapping;
using DataEntity = Database.Entity;

public class OuterCustomerMapping : IMappingRegistration
{
    public void RegisterNodeMap(Dictionary<string, NodeMap> mappings)
    {
        var cust = new NodeMap
        {
            Alias = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer),
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
                    ToColumn = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerId)
                },
                new LinkKey()
                {
                    From = nameof(DataEntity.Customer),
                    FromColumn = nameof(DataEntity.Customer.Id),
                    To = nameof(DataEntity.CustomerCustomerRelationship),
                    ToColumn = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerId)
                }
            },
            ModelToEntityLinks =
            {
                new LinkKey()
                {
                    From = nameof(Customer),
                    FromColumn = nameof(Customer.CustomerKey),
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
            new List<KeyValuePair<string, (string, int)>>()
            {
                new($"{nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer)}~{nameof(DataEntity.Customer)}~{nameof(DataEntity.CustomerType)}", (CustomerType.Person.ToString(), (int)CustomerType.Person)),
                new($"{nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer)}~{nameof(DataEntity.Customer)}~{nameof(DataEntity.CustomerType)}", (CustomerType.Organisation.ToString(), (int)CustomerType.Organisation)),
            },
            new List<KeyValuePair<string, (string, int)>>()
            {
                new($"{nameof(DataEntity.Customer)}~{nameof(Customer)}~{nameof(DataEntity.CustomerType)}", (DataEntity.CustomerType.Person.ToString(), (int)CustomerType.Person)),
                new($"{nameof(DataEntity.Customer)}~{nameof(Customer)}~{nameof(DataEntity.CustomerType)}", (DataEntity.CustomerType.Organisation.ToString(), (int)CustomerType.Organisation))
            });

        cust.FromEnum = custEnums.from;
        cust.ToEnum = custEnums.to;
        
        mappings.TryAdd(nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer), MappingRegistry.Register(typeof(Customer), typeof(DataEntity.Customer), cust, nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer)));
    }

    public void Register(Dictionary<string, NodeMap> mappings)
    {
        RegisterNodeMap(mappings);
    }
}