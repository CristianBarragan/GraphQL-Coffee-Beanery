using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using DataEntity = Database.Entity;

namespace Domain.Shared.Mapping;

public class ContractMappingSet : IMappingSet<CustomerMappingType>
{
    public void Register(CustomerMappingType type)
    {
        new ContractMapping(type.ToString()).Register();
    }
}

public class ContractMapping : BaseMappingRegistration<Contract, DataEntity.Contract>
{
    public ContractMapping(string alias) : base(alias)
    {
    }

    protected override NodeMap BuildMap()
    {
        var map = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Lending)
        };

        map.EntityParents.Add(new LinkKey
        {
            From       = nameof(DataEntity.Contract),
            FromColumn = nameof(DataEntity.Contract.CustomerBankingRelationshipId),
            To         = nameof(DataEntity.CustomerBankingRelationship),
            ToColumn   = nameof(DataEntity.CustomerBankingRelationship.Id)
        });

        map.EntityRelatedChildren.Add(new LinkKey
        {
            From       = nameof(DataEntity.Contract),
            FromColumn = nameof(DataEntity.Contract.AccountId),
            To         = nameof(DataEntity.Account),
            ToColumn   = nameof(DataEntity.Account.Id)
        });

        map.ModelParents.Add(new LinkKey
        {
            From       = nameof(Contract),
            FromColumn = nameof(Contract.CustomerBankingRelationshipKey),
            To         = nameof(CustomerBankingRelationship),
            ToColumn   = nameof(CustomerBankingRelationship.CustomerBankingRelationshipKey)
        });

        map.ModelChildren.Add(new LinkKey
        {
            From       = nameof(Contract),
            FromColumn = nameof(Contract.ContractKey),
            To         = nameof(Transaction),
            ToColumn   = nameof(Transaction.ContractKey)
        });

        map.ModelToEntityLinks.Add(new LinkKey
        {
            From       = nameof(Contract),
            FromColumn = nameof(Contract.ContractKey),
            To         = nameof(DataEntity.Contract),
            ToColumn   = nameof(DataEntity.Contract.ContractKey)
        });

        map.UpsertKeys.Add(new UpsertKey(
            nameof(DataEntity.Contract),
            nameof(DataEntity.Contract.ContractKey)
        ));

        map.FieldMaps.AddRange(new[]
        {
            new FieldMap { SourceName = nameof(DataEntity.Contract.Id), DestinationEntity = nameof(DataEntity.Contract), DestinationName = nameof(DataEntity.Contract.Id) },
            new FieldMap { SourceName = nameof(Contract.ContractKey),   DestinationEntity = nameof(DataEntity.Contract), DestinationName = nameof(DataEntity.Contract.ContractKey) },
            new FieldMap
            {
                SourceName = nameof(Product.ProductType),  DestinationEntity = nameof(DataEntity.Contract), DestinationName = nameof(DataEntity.Contract.ContractType),
                FromEnum = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { ContractType.CreditCard.ToString(), (int)ContractType.CreditCard },
                    { ContractType.Mortgage.ToString(), (int)ContractType.Mortgage },
                    { ContractType.PersonalLoan.ToString(), (int)ContractType.PersonalLoan }
                },
                ToEnum = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { ProductType.CreditCard.ToString(), (int)ProductType.CreditCard },
                    { ProductType.Mortgage.ToString(), (int)ProductType.Mortgage },
                    { ProductType.PersonalLoan.ToString(), (int)ProductType.PersonalLoan }
                }
            },
            new FieldMap { SourceName = nameof(Contract.Amount),        DestinationEntity = nameof(DataEntity.Contract), DestinationName = nameof(DataEntity.Contract.Amount) }
        });

        return map;
    }
}