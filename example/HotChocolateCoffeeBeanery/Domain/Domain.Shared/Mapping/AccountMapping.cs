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
            Schema = nameof(DataEntity.Schema.Account),
            EntityRelatedParents = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(DataEntity.Account),
                    FromColumn = nameof(DataEntity.Account.Id),
                    To = nameof(DataEntity.Contract),
                    ToColumn = nameof(DataEntity.Contract.AccountId)
                }
            },
            EntityChildren = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(DataEntity.Account),
                    FromColumn = nameof(DataEntity.Account.Id),
                    To = nameof(DataEntity.Transaction),
                    ToColumn = nameof(DataEntity.Transaction.AccountId)
                }
            },
            ModelParents = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(Account),
                    FromColumn = nameof(DataEntity.Account.AccountKey),
                    To = nameof(Product),
                    ToColumn = nameof(Product.AccountKey)
                } 
            },
            ModelToEntityLinks =
            {
                new LinkKey()
                {
                    From = nameof(Account),
                    FromColumn = nameof(Account.AccountKey),
                    To = nameof(DataEntity.Account),
                    ToColumn = nameof(DataEntity.Account.AccountKey)
                }
            }
        };

        acct.IsEntity = true;
        acct.IsModel = true;

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