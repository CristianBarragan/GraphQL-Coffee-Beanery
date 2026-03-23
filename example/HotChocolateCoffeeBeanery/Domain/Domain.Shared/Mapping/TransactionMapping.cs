using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;

namespace Domain.Shared.Mapping;
using DataEntity = Database.Entity;

    public class TransactionMapping : IMappingRegistration
    {
        public void RegisterNodeMap(Dictionary<string, NodeMap> mappings)
        {
            var transaction = new NodeMap
            {
                Schema = nameof(DataEntity.Schema.Lending)
            };

            transaction.IsEntity = true;
            transaction.IsModel = true;

            transaction.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.Transaction),
                nameof(DataEntity.Transaction.TransactionKey)));

            transaction.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(DataEntity.Transaction.Id),
                DestinationEntity = nameof(DataEntity.Transaction),
                DestinationName = nameof(DataEntity.Transaction.Id)
            });

            transaction.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Transaction.TransactionKey),
                DestinationEntity = nameof(DataEntity.Transaction),
                DestinationName = nameof(DataEntity.Transaction.TransactionKey)
            });

            transaction.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Transaction.Amount),
                DestinationEntity = nameof(DataEntity.Transaction),
                DestinationName = nameof(DataEntity.Transaction.Amount)
            });

            transaction.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Transaction.Balance),
                DestinationEntity = nameof(DataEntity.Transaction),
                DestinationName = nameof(DataEntity.Transaction.Balance)
            });

            mappings.TryAdd(nameof(Transaction), MappingRegistry.Register(typeof(Transaction), typeof(DataEntity.Transaction), transaction));
        }

        public void Register(Dictionary<string, NodeMap> mappings)
        {
            RegisterNodeMap(mappings);
        }
    }
