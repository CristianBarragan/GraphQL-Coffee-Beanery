using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;

namespace Domain.Shared.Mapping;
using DataEntity = Database.Entity;

public class AccountMapping : IMappingRegistration
{
    public void RegisterNodeMap(Dictionary<string, NodeMap> mappings)
    {
        var acct = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Account)
        };

        acct.IsEntity = true;

        acct.Children.Add(nameof(DataEntity.Contract));
        acct.Children.Add(nameof(DataEntity.Transaction));

        acct.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.Account), nameof(DataEntity.Account.AccountKey)));

        acct.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(DataEntity.Account.Id),
            DestinationEntity = nameof(DataEntity.Account),
            DestinationName = nameof(DataEntity.Account.Id)
        });

        acct.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Account.AccountKey),
            DestinationEntity = nameof(DataEntity.Account),
            DestinationName = nameof(DataEntity.Account.AccountKey)
        });

        acct.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Account.AccountNumber),
            DestinationEntity = nameof(DataEntity.Account),
            DestinationName = nameof(DataEntity.Account.AccountNumber)
        });

        acct.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(Account.AccountName),
            DestinationEntity = nameof(DataEntity.Account),
            DestinationName = nameof(DataEntity.Account.AccountName)
        });
        
        mappings.TryAdd(nameof(Account), MappingRegistry.Register(typeof(Account), typeof(DataEntity.Account), acct));
    }

    public void Register(Dictionary<string, NodeMap> mappings)
    {
        RegisterNodeMap(mappings);
    }
}