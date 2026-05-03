using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using DataEntity = Database.Entity;

namespace Domain.Shared.Mapping;

public class ProductMapping : BaseModelMappingRegistration<Product>
{
    protected override NodeMap BuildMap()
    {
        var map = new NodeMap
        {
            Alias = nameof(Product)
        };

        map.ModelChildren.AddRange(new[]
        {
            new LinkKey { From = nameof(Product), FromColumn = nameof(Product.ContractKey),                    To = nameof(Contract),                   ToColumn = nameof(Contract.ContractKey) },
            new LinkKey { From = nameof(Product), FromColumn = nameof(Product.AccountKey),                     To = nameof(Account),                    ToColumn = nameof(Account.AccountKey) },
            new LinkKey { From = nameof(Product), FromColumn = nameof(Product.CustomerBankingRelationshipKey), To = nameof(CustomerBankingRelationship), ToColumn = nameof(CustomerBankingRelationship.CustomerBankingRelationshipKey) }
        });

        map.ModelParents.Add(new LinkKey
        {
            From = nameof(Product), FromColumn = nameof(Product.CustomerKey),
            To   = nameof(Customer), ToColumn  = nameof(Customer.CustomerKey)
        });

        map.ModelToEntityLinks.AddRange(new[]
        {
            new LinkKey { From = nameof(Product), FromColumn = nameof(Product.CustomerBankingRelationshipKey), To = nameof(DataEntity.CustomerBankingRelationship), ToColumn = nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey) },
            new LinkKey { From = nameof(Product), FromColumn = nameof(Product.ContractKey),                    To = nameof(DataEntity.Contract),                    ToColumn = nameof(DataEntity.Contract.ContractKey) },
            new LinkKey { From = nameof(Product), FromColumn = nameof(Product.AccountKey),                     To = nameof(DataEntity.Account),                     ToColumn = nameof(DataEntity.Account.AccountKey) }
        });

        map.FieldMaps.AddRange(new[]
        {
            new FieldMap { SourceName = nameof(Product.ContractKey),                    DestinationEntity = nameof(DataEntity.Contract),                    DestinationName = nameof(DataEntity.Contract.ContractKey) },
            new FieldMap
            {
                SourceName = nameof(Product.ProductType),  DestinationEntity = nameof(DataEntity.Contract), DestinationName = nameof(DataEntity.Contract.ContractType),
                FromEnum = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { ProductType.CreditCard.ToString(), (int)ProductType.CreditCard },
                    { ProductType.Mortgage.ToString(), (int)ProductType.Mortgage },
                    { ProductType.PersonalLoan.ToString(), (int)ProductType.PersonalLoan }
                },
                ToEnum = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { ContractType.CreditCard.ToString(), (int)ContractType.CreditCard },
                    { ContractType.Mortgage.ToString(), (int)ContractType.Mortgage },
                    { ContractType.PersonalLoan.ToString(), (int)ContractType.PersonalLoan }
                }
            },
            new FieldMap { SourceName = nameof(Product.CustomerKey),                    DestinationEntity = nameof(DataEntity.Customer),                    DestinationName = nameof(DataEntity.Customer.CustomerKey) },
            new FieldMap { SourceName = nameof(Product.AccountKey),                     DestinationEntity = nameof(DataEntity.Account),                     DestinationName = nameof(DataEntity.Account.AccountKey) },
            new FieldMap { SourceName = nameof(Product.AccountName),                    DestinationEntity = nameof(DataEntity.Account),                     DestinationName = nameof(DataEntity.Account.AccountName) },
            new FieldMap { SourceName = nameof(Product.AccountNumber),                  DestinationEntity = nameof(DataEntity.Account),                     DestinationName = nameof(DataEntity.Account.AccountNumber) },
            new FieldMap { SourceName = nameof(Product.Amount),                         DestinationEntity = nameof(DataEntity.Transaction),                 DestinationName = nameof(DataEntity.Transaction.Amount) },
            new FieldMap { SourceName = nameof(Product.Balance),                        DestinationEntity = nameof(DataEntity.Transaction),                 DestinationName = nameof(DataEntity.Transaction.Balance) },
            new FieldMap { SourceName = nameof(Product.CustomerBankingRelationshipKey), DestinationEntity = nameof(DataEntity.CustomerBankingRelationship), DestinationName = nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey) }
        });

        return map;
    }
}