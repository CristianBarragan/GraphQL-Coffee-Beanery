using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;

namespace Domain.Shared.Mapping;
using DataEntity = Database.Entity;

    public class TransactionMapping 
    : BaseMappingRegistration<Transaction, DataEntity.Transaction>
{
    protected override string Alias => nameof(Transaction);

    protected override NodeMap BuildMap()
    {
        var map = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Lending)
        };

        map.EntityParents.Add(new LinkKey
        {
            From       = nameof(DataEntity.Transaction),
            FromColumn = nameof(DataEntity.Transaction.ContractId),
            To         = nameof(DataEntity.Contract),
            ToColumn   = nameof(DataEntity.Contract.Id)
        });

        map.EntityRelatedParents.Add(new LinkKey
        {
            From       = nameof(DataEntity.Transaction),
            FromColumn = nameof(DataEntity.Transaction.AccountId),
            To         = nameof(DataEntity.Account),
            ToColumn   = nameof(DataEntity.Account.Id)
        });

        map.ModelParents.AddRange(new[]
        {
            new LinkKey { From = nameof(Transaction), FromColumn = nameof(Transaction.ContractKey), To = nameof(Contract), ToColumn = nameof(Contract.ContractKey) },
            new LinkKey { From = nameof(Transaction), FromColumn = nameof(Transaction.AccountKey),  To = nameof(Account),  ToColumn = nameof(Account.AccountKey) }
        });

        map.ModelToEntityLinks.Add(new LinkKey
        {
            From       = nameof(Transaction),
            FromColumn = nameof(Transaction.TransactionKey),
            To         = nameof(DataEntity.Transaction),
            ToColumn   = nameof(DataEntity.Transaction.TransactionKey)
        });

        map.UpsertKeys.Add(new UpsertKey(
            nameof(DataEntity.Transaction),
            nameof(DataEntity.Transaction.TransactionKey)
        ));

        map.FieldMaps.AddRange(new[]
        {
            new FieldMap { SourceName = nameof(DataEntity.Transaction.Id),  DestinationEntity = nameof(DataEntity.Transaction), DestinationName = nameof(DataEntity.Transaction.Id) },
            new FieldMap { SourceName = nameof(Transaction.TransactionKey), DestinationEntity = nameof(DataEntity.Transaction), DestinationName = nameof(DataEntity.Transaction.TransactionKey) },
            new FieldMap { SourceName = nameof(Transaction.Amount),         DestinationEntity = nameof(DataEntity.Transaction), DestinationName = nameof(DataEntity.Transaction.Amount) },
            new FieldMap { SourceName = nameof(Transaction.Balance),        DestinationEntity = nameof(DataEntity.Transaction), DestinationName = nameof(DataEntity.Transaction.Balance) }
        });

        return map;
    }
}
