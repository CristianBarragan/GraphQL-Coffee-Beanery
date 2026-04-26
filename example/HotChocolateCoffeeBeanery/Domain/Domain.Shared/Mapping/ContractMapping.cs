using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;

namespace Domain.Shared.Mapping;
using DataEntity = Database.Entity;

public class ContractMapping : IMappingRegistration
{
    public void RegisterNodeMap(Dictionary<string, NodeMap> mappings)
    {
        var contract = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Lending),
            EntityParents = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(DataEntity.Contract),
                    FromColumn = nameof(DataEntity.Contract.CustomerBankingRelationshipId),
                    To = nameof(DataEntity.CustomerBankingRelationship),
                    ToColumn = nameof(DataEntity.CustomerBankingRelationship.Id)
                }
            },
            EntityRelatedChildren = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(DataEntity.Contract),
                    FromColumn = nameof(DataEntity.Contract.AccountId),
                    To = nameof(DataEntity.Account),
                    ToColumn = nameof(DataEntity.Account.Id)
                }
            },
            ModelParents = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(Contract),
                    FromColumn = nameof(Contract.CustomerBankingRelationshipKey),
                    To = nameof(CustomerBankingRelationship),
                    ToColumn = nameof(CustomerBankingRelationship.CustomerBankingRelationshipKey)
                }
            },
            ModelChildren = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(Contract),
                    FromColumn = nameof(Contract.ContractKey),
                    To = nameof(Transaction),
                    ToColumn = nameof(Transaction.ContractKey)
                }
            },
            ModelToEntityLinks =
            {
                new LinkKey()
                {
                    From = nameof(Contract),
                    FromColumn = nameof(Contract.ContractKey),
                    To = nameof(DataEntity.Contract),
                    ToColumn = nameof(DataEntity.Contract.ContractKey)
                }
            }
        };

        contract.IsEntity = true;
        contract.IsModel = true;

        contract.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.Contract),
            nameof(DataEntity.Contract.ContractKey)));

        contract.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(DataEntity.Contract.Id),
            DestinationEntity = nameof(DataEntity.Contract),
            DestinationName = nameof(DataEntity.Contract.Id)
        });

        contract.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Contract.ContractKey),
            DestinationEntity = nameof(DataEntity.Contract),
            DestinationName = nameof(DataEntity.Contract.ContractKey)
        });

        contract.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Contract.Amount),
            DestinationEntity = nameof(DataEntity.Contract),
            DestinationName = nameof(DataEntity.Contract.Amount)
        });
        
        // Enum mapping for ContractType
        var contractEnums = EnumMapFactory.Create(
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

        contract.FromEnum = contractEnums.from;
        contract.ToEnum = contractEnums.to;
        
        mappings.TryAdd(nameof(Contract), MappingRegistry.Register(typeof(Contract), typeof(DataEntity.Contract), contract));
    }

    public void Register(Dictionary<string, NodeMap> mappings)
    {
        RegisterNodeMap(mappings);
    }
}