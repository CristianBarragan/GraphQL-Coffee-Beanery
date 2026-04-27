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
            ModelChildren = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(Product),
                    FromColumn = nameof(Product.ContractKey),
                    To = nameof(Contract),
                    ToColumn = nameof(Contract.ContractKey)
                },
                new LinkKey()
                {
                    From = nameof(Product),
                    FromColumn = nameof(Product.AccountKey),
                    To = nameof(Account),
                    ToColumn = nameof(Account.AccountKey)
                },
                new LinkKey()
                {
                    From = nameof(Product),
                    FromColumn = nameof(Product.CustomerBankingRelationshipKey),
                    To = nameof(CustomerBankingRelationship),
                    ToColumn = nameof(CustomerBankingRelationship.CustomerBankingRelationshipKey)
                }
            },
            ModelParents = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(Product),
                    FromColumn = nameof(Product.CustomerKey),
                    To = nameof(Customer),
                    ToColumn = nameof(Customer.CustomerKey)
                }
            },
            ModelToEntityLinks =
            {
                new LinkKey()
                {
                    From = nameof(Product),
                    FromColumn = nameof(Product.CustomerBankingRelationshipKey),
                    To = nameof(DataEntity.CustomerBankingRelationship),
                    ToColumn = nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey)
                },
                new LinkKey()
                {
                    From = nameof(Product),
                    FromColumn = nameof(Product.ContractKey),
                    To = nameof(DataEntity.Contract),
                    ToColumn = nameof(DataEntity.Contract.ContractKey)
                },
                new LinkKey()
                {
                    From = nameof(Product),
                    FromColumn = nameof(Product.AccountKey),
                    To = nameof(DataEntity.Account),
                    ToColumn = nameof(DataEntity.Account.AccountKey)
                }
            }
        };

        product.IsModel = true;

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
        
        product.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Product.Amount),
            DestinationEntity = nameof(DataEntity.Transaction),
            DestinationName = nameof(DataEntity.Transaction.Amount)
        });
        
        product.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Product.Amount),
            DestinationEntity = nameof(DataEntity.Transaction),
            DestinationName = nameof(DataEntity.Transaction.Balance)
        });

        // Enum mapping for ProductType by value
        var productEnums = EnumMapFactory.Create(
            new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
            {
                { $"{nameof(Product)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContractType.CreditCard}", (ProductType.CreditCard.ToString(), (int)DataEntity.ContractType.CreditCard) },
                { $"{nameof(Product)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContractType.Mortgage}", (ProductType.Mortgage.ToString(), (int)DataEntity.ContractType.Mortgage) },
                { $"{nameof(Product)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContractType.PersonalLoan}", (ProductType.PersonalLoan.ToString(), (int)DataEntity.ContractType.PersonalLoan) }
            },
            new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
            {
                { $"{nameof(DataEntity.ContactPoint)}~{nameof(Product)}~{ProductType.CreditCard}", (DataEntity.ContractType.CreditCard.ToString(), (int)ProductType.CreditCard) },
                { $"{nameof(DataEntity.ContactPoint)}~{nameof(Product)}~{ProductType.Mortgage}", (DataEntity.ContractType.Mortgage.ToString(), (int)ProductType.Mortgage) },
                { $"{nameof(DataEntity.ContactPoint)}~{nameof(Product)}~{ProductType.PersonalLoan}", (DataEntity.ContractType.PersonalLoan.ToString(), (int)ProductType.PersonalLoan) }
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