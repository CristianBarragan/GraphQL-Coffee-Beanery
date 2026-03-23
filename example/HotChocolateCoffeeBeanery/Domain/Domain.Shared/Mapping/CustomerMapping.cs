using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;

namespace Domain.Shared.Mapping;
using DataEntity = Database.Entity;

public class CustomerMapping : IMappingRegistration
{
    public void RegisterNodeMap(Dictionary<string, NodeMap> mappings)
    {
        var cust = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Banking)
        };

        cust.IsEntity = true;
        cust.IsModel = true;

        cust.Children.Add(nameof(DataEntity.ContactPoint));
        cust.Children.Add(nameof(DataEntity.CustomerBankingRelationship));

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
            new Dictionary<string, (CustomerType, DataEntity.CustomerType)>
            {
                { $"{nameof(Customer)}~{nameof(DataEntity.Customer)}~{DataEntity.CustomerType.Person}", (CustomerType.Person, DataEntity.CustomerType.Person) },
                { $"{nameof(Customer)}~{nameof(DataEntity.Customer)}~{DataEntity.CustomerType.Organisation}", (CustomerType.Organisation, DataEntity.CustomerType.Organisation) }
            },
            new Dictionary<string, (DataEntity.CustomerType, CustomerType)>
            {
                { $"{nameof(DataEntity.Customer)}~{nameof(Customer)}~{CustomerType.Person}", (DataEntity.CustomerType.Person, CustomerType.Person) },
                { $"{nameof(DataEntity.Customer)}~{nameof(Customer)}~{CustomerType.Organisation}", (DataEntity.CustomerType.Organisation, CustomerType.Organisation) }
            });

        cust.FromEnum = custEnums.from;
        cust.ToEnum = custEnums.to;

        mappings.TryAdd(nameof(Customer), MappingRegistry.Register(typeof(Customer), typeof(DataEntity.Customer), cust, nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer)));
        mappings.TryAdd(nameof(Customer), MappingRegistry.Register(typeof(Customer), typeof(DataEntity.Customer), cust, nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer)));
    }

    public void Register(Dictionary<string, NodeMap> mappings)
    {
        RegisterNodeMap(mappings);
    }
}