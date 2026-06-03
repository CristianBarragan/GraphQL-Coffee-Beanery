using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using DataEntity = Database.Entity;

namespace Domain.Shared.Mapping;

public class AccountMappingSet : IMappingSet<CustomerMappingType, Domain.Model.Model>
{
    public void Register(CustomerMappingType type, Domain.Model.Model model)
    {
        new AccountMapping(type.ToString(), model.ToString()).Register();
    }
}

public class AccountMapping : BaseMappingRegistration<Account, DataEntity.Account>
{
    public AccountMapping(string alias, string model) : base(alias, model)
    {
    }

    protected override NodeMap BuildMap()
    {
        var map = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Account)
        };

        map.EntityRelatedParents.AddRange(new[]
        {
            new LinkKey
            {
                AliasFrom    = A(nameof(DataEntity.Account)),
                From       = nameof(DataEntity.Account),
                FromColumn = nameof(DataEntity.Account.Id),
                AliasTo    = A(nameof(Contract)),
                To         = nameof(Contract),
                ToColumn   = nameof(DataEntity.Contract.AccountId)
            },
            new LinkKey
            {
                AliasFrom    = A(nameof(DataEntity.Account)),
                From       = nameof(DataEntity.Account),
                FromColumn = nameof(DataEntity.Account.AccountKey),
                AliasTo    = A(nameof(Contract)),
                To         = nameof(Contract),
                ToColumn   = nameof(DataEntity.Contract.AccountKey)
            }
        });

        map.EntityChildren.Add(new LinkKey
        {
            AliasFrom    = A(nameof(DataEntity.Account)),
            From       = nameof(DataEntity.Account),
            FromColumn = nameof(DataEntity.Account.Id),
            AliasTo    = A(nameof(DataEntity.Transaction)),
            To         = nameof(DataEntity.Transaction),
            ToColumn   = nameof(DataEntity.Transaction.AccountId)
        });

        map.ModelParents.Add(new LinkKey
        {
            AliasFrom    = A(nameof(Account)),
            From       = nameof(Account),
            FromColumn = nameof(DataEntity.Account.AccountKey),
            AliasTo    = A(nameof(Product)),
            To         = nameof(Product),
            ToColumn   = nameof(Product.AccountKey)
        });

        map.ModelToEntityLinks.Add(new LinkKey
        {
            AliasFrom    = A(nameof(Account)),
            From       = nameof(Account),
            FromColumn = nameof(Account.AccountKey),
            AliasTo    = A(nameof(DataEntity.Account)),
            To         = nameof(DataEntity.Account),
            ToColumn   = nameof(DataEntity.Account.AccountKey)
        });

        map.UpsertKeys.Add(new UpsertKey(
            nameof(DataEntity.Account),
            nameof(DataEntity.Account.AccountKey)
        ));

        map.FieldMaps.AddRange(new[]
        {
            new FieldMap { SourceAlias = A(nameof(DataEntity.Account)), DestinationAlias = A(nameof(DataEntity.Account)), SourceName = nameof(DataEntity.Account.Id), DestinationEntity = nameof(DataEntity.Account), DestinationName = nameof(DataEntity.Account.Id) },
            new FieldMap { SourceAlias = A(nameof(DataEntity.Account)), DestinationAlias = A(nameof(DataEntity.Account)), SourceName = nameof(Account.AccountKey), DestinationEntity = nameof(DataEntity.Account), DestinationName = nameof(DataEntity.Account.AccountKey) },
            new FieldMap { SourceAlias = A(nameof(DataEntity.Account)), DestinationAlias = A(nameof(DataEntity.Account)), SourceName = nameof(Account.AccountNumber), DestinationEntity = nameof(DataEntity.Account), DestinationName = nameof(DataEntity.Account.AccountNumber) },
            new FieldMap { SourceAlias = A(nameof(DataEntity.Account)), DestinationAlias = A(nameof(DataEntity.Account)), SourceName = nameof(Account.AccountName), DestinationEntity = nameof(DataEntity.Account), DestinationName = nameof(DataEntity.Account.AccountName) }
        });

        return map;
    }
}