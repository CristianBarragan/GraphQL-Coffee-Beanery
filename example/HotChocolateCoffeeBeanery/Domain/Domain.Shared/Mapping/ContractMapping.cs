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
            Schema = nameof(DataEntity.Schema.Lending)
        };

        contract.IsEntity = true;

        contract.Children.Add(nameof(DataEntity.Account));
        contract.Children.Add(nameof(DataEntity.Transaction));

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

        contract.FromEnum = contractEnums.from;
        contract.ToEnum = contractEnums.to;
        
        mappings.TryAdd(nameof(Contract), MappingRegistry.Register(typeof(Contract), typeof(DataEntity.Contract), contract));
    }

    public void Register(Dictionary<string, NodeMap> mappings)
    {
        RegisterNodeMap(mappings);
    }
}