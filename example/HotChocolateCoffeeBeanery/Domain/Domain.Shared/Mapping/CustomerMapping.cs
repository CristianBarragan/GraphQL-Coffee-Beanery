using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using DataEntity = Database.Entity;

namespace Domain.Shared.Mapping;

public class InnerCustomerMappingSet : IMappingSet<CustomerMappingType>
{
    public void Register(CustomerMappingType type)
    {
        new InnerCustomerMapping(type.ToString()).Register();
    }
}

public class OuterCustomerMappingSet : IMappingSet<CustomerMappingType>
{
    public void Register(CustomerMappingType type)
    {
        new OuterCustomerMapping(type.ToString()).Register();
    }
}

public class InnerCustomerMapping : CustomerBaseMapping<InnerCustomerMapping>
{
    public InnerCustomerMapping(string alias) : base(alias)
    {
    }

    protected override NodeMap BuildMap()
    {
        var map = base.BuildMap();
        
        map.EntityParents.Add(new LinkKey
         {
             From       = nameof(DataEntity.Customer),
             FromColumn = nameof(DataEntity.Customer.Id),
             AliasTo = A(nameof(DataEntity.CustomerCustomerRelationship)),
             To         = nameof(DataEntity.CustomerCustomerRelationship),
             ToColumn   = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerId)
         });

         map.ModelToEntityLinks.AddRange(new[]
         {
             new LinkKey { From = nameof(CustomerCustomerEdge.InnerCustomer), FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey), AliasTo = A(nameof(DataEntity.Customer)), 
                 To = nameof(DataEntity.Customer), ToColumn = nameof(DataEntity.Customer.CustomerKey) },
         });
         
         return map;
    }
}

public class OuterCustomerMapping : CustomerBaseMapping<OuterCustomerMapping>
{
    public OuterCustomerMapping(string alias) : base(alias)
    {
    }
    
    protected override NodeMap BuildMap()
    {
        var map = base.BuildMap();
        
        map.EntityParents.Add(new LinkKey
        {
            From       = nameof(DataEntity.Customer),
            FromColumn = nameof(DataEntity.Customer.Id),
            AliasTo = A(nameof(DataEntity.CustomerCustomerRelationship)),
            To         = nameof(DataEntity.CustomerCustomerRelationship),
            ToColumn   = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerId)
        });

        map.ModelToEntityLinks.AddRange(new[]
        {
            new LinkKey { From = nameof(CustomerCustomerEdge.OuterCustomer), FromColumn = nameof(CustomerCustomerEdge.OuterCustomerKey), 
                AliasTo = A(nameof(DataEntity.Customer)), To = nameof(DataEntity.Customer), ToColumn = nameof(DataEntity.Customer.CustomerKey) },
        });
         
        return map;
    }
}

public abstract class CustomerBaseMapping<TAlias>
    : BaseMappingRegistration<Customer, DataEntity.Customer>
{
    protected CustomerBaseMapping(string alias) : base(alias)
    {
    }

    protected override NodeMap BuildMap()
    {
        var map = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Banking)
        };
        
        map.EntityChildren.AddRange(new[]
        {
            new LinkKey
            {
                AliasFrom    = A(nameof(DataEntity.Customer)), 
                From       = nameof(DataEntity.Customer),
                FromColumn = nameof(DataEntity.Customer.Id),
                AliasTo    = A(nameof(DataEntity.ContactPoint)),
                To         = nameof(DataEntity.ContactPoint),
                ToColumn   = nameof(DataEntity.ContactPoint.CustomerId)
            },
            new LinkKey
            {
                AliasFrom    = A(nameof(DataEntity.Customer)),
                From       = nameof(DataEntity.Customer),
                FromColumn = nameof(DataEntity.Customer.Id),
                AliasTo    = A(nameof(DataEntity.CustomerBankingRelationship)),
                To         = nameof(DataEntity.CustomerBankingRelationship),
                ToColumn   = nameof(DataEntity.CustomerBankingRelationship.CustomerId)
            }
        });

        map.UpsertKeys.Add(new UpsertKey(
            nameof(DataEntity.Customer),
            nameof(DataEntity.Customer.CustomerKey)
        ));

        map.FieldMaps.AddRange(new[]
        {
            new FieldMap { SourceName = nameof(DataEntity.Customer.Id), DestinationEntity = nameof(DataEntity.Customer), DestinationName = nameof(DataEntity.Customer.Id) },
            new FieldMap { SourceName = nameof(Customer.CustomerKey),   DestinationEntity = nameof(DataEntity.Customer), DestinationName = nameof(DataEntity.Customer.CustomerKey) },
            new FieldMap
            {
                SourceName        = nameof(Customer.CustomerType),
                DestinationEntity = nameof(DataEntity.Customer),
                DestinationName   = nameof(DataEntity.Customer.CustomerType),
                FromEnum = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { CustomerType.Person.ToString(),       (int)CustomerType.Person },
                    { CustomerType.Organisation.ToString(), (int)CustomerType.Organisation }
                },
                ToEnum = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { DataEntity.CustomerType.Person.ToString(),       (int)DataEntity.CustomerType.Person },
                    { DataEntity.CustomerType.Organisation.ToString(), (int)DataEntity.CustomerType.Organisation }
                }
            },
            new FieldMap { SourceName = nameof(Customer.FirstNaming), DestinationEntity = nameof(DataEntity.Customer), DestinationName = nameof(DataEntity.Customer.FirstName) },
            new FieldMap { SourceName = nameof(Customer.LastNaming),  DestinationEntity = nameof(DataEntity.Customer), DestinationName = nameof(DataEntity.Customer.LastName) },
            new FieldMap { SourceName = nameof(Customer.FullNaming),  DestinationEntity = nameof(DataEntity.Customer), DestinationName = nameof(DataEntity.Customer.FullName) }
        });

        return map;
    }
}