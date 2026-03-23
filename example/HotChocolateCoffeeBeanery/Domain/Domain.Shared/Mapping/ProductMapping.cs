using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;

namespace Domain.Shared.Mapping;
using DataEntity = Database.Entity;

public class ProductMapping : IMappingRegistration
{
    public void RegisterNodeMap(Dictionary<string, NodeMap> mappings)
    {
        var product = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Banking)
        };

        product.IsModel = true;

        product.Children.Add(nameof(Contract));
        product.Children.Add(nameof(Account));

        product.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Product.ContractKey),
            DestinationEntity = nameof(DataEntity.Contract),
            DestinationName = nameof(DataEntity.Contract.ContractKey)
        });

        product.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Product.CustomerKey),
            DestinationEntity = nameof(DataEntity.Customer),
            DestinationName = nameof(DataEntity.Customer.CustomerKey)
        });

        product.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Product.AccountKey),
            DestinationEntity = nameof(DataEntity.Account),
            DestinationName = nameof(DataEntity.Account.AccountKey)
        });

        product.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Product.AccountName),
            DestinationEntity = nameof(DataEntity.Account),
            DestinationName = nameof(DataEntity.Account.AccountName)
        });

        product.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Product.AccountNumber),
            DestinationEntity = nameof(DataEntity.Account),
            DestinationName = nameof(DataEntity.Account.AccountNumber)
        });

        // Enum mapping for ProductType by value
        var productEnums = EnumMapFactory.Create(
            new Dictionary<string, (ProductType, DataEntity.ContractType)>
            {
                { $"{nameof(Product)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContractType.CreditCard}", (ProductType.CreditCard, DataEntity.ContractType.CreditCard) },
                { $"{nameof(Product)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContractType.Mortgage}", (ProductType.Mortgage, DataEntity.ContractType.Mortgage) },
                { $"{nameof(Product)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContractType.PersonalLoan}", (ProductType.PersonalLoan, DataEntity.ContractType.PersonalLoan) }
            },
            new Dictionary<string, (DataEntity.ContractType, ProductType)>
            {
                { $"{nameof(DataEntity.ContactPoint)}~{nameof(Product)}~{ProductType.CreditCard}", (DataEntity.ContractType.CreditCard, ProductType.CreditCard) },
                { $"{nameof(DataEntity.ContactPoint)}~{nameof(Product)}~{ProductType.Mortgage}", (DataEntity.ContractType.Mortgage, ProductType.Mortgage) },
                { $"{nameof(DataEntity.ContactPoint)}~{nameof(Product)}~{ProductType.PersonalLoan}", (DataEntity.ContractType.PersonalLoan, ProductType.PersonalLoan) }
            });

        product.FromEnum = productEnums.from;
        product.ToEnum = productEnums.to;
        
        mappings.TryAdd(nameof(Product), MappingRegistry.Register(typeof(Product), null, product));
    }

    public void Register(Dictionary<string, NodeMap> mappings)
    {
        RegisterNodeMap(mappings);
    }
}