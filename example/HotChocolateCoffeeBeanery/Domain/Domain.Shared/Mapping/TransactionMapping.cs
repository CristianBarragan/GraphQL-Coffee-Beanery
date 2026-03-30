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
                Schema = nameof(DataEntity.Schema.Lending),
                EntityParents = new List<LinkKey>()
                {
                    new LinkKey()
                    {
                        From = nameof(DataEntity.Transaction),
                        FromColumn = nameof(DataEntity.Transaction.ContractId),
                        To = nameof(DataEntity.Contract),
                        ToColumn = nameof(DataEntity.Contract.Id)
                    }
                },
                EntityRelatedChildren = new List<LinkKey>()
                {
                    new LinkKey()
                    {
                        From = nameof(DataEntity.Transaction),
                        FromColumn = nameof(DataEntity.Transaction.AccountId),
                        To = nameof(DataEntity.Account),
                        ToColumn = nameof(DataEntity.Account.Id)
                    }
                },
                ModelParents = new List<LinkKey>()
                {
                    new LinkKey()
                    {
                        From = nameof(Transaction),
                        FromColumn = nameof(Transaction.ContractKey),
                        To = nameof(Contract),
                        ToColumn = nameof(Contract.ContractKey)
                    },
                    new LinkKey()
                    {
                        From = nameof(Transaction),
                        FromColumn = nameof(Transaction.AccountKey),
                        To = nameof(Account),
                        ToColumn = nameof(Account.AccountKey)
                    }
                },
                ModelToEntityLinks =
                {
                    new LinkKey()
                    {
                        From = nameof(Transaction),
                        FromColumn = nameof(Transaction.TransactionKey),
                        To = nameof(DataEntity.Transaction),
                        ToColumn = nameof(DataEntity.Transaction.TransactionKey)
                    }
                }
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
