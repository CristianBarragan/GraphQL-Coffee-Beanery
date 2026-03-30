using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;

namespace Domain.Shared.Mapping;
using DataEntity = Database.Entity;

public class CustomerBankingRelationshipMapping : IMappingRegistration
{
    public void RegisterNodeMap(Dictionary<string, NodeMap> mappings)
    {
        var cbr = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Banking),
            EntityParents = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(DataEntity.CustomerBankingRelationship),
                    FromColumn = nameof(DataEntity.CustomerBankingRelationship.CustomerId),
                    To = nameof(DataEntity.Customer),
                    ToColumn = nameof(DataEntity.Customer.Id)
                }
            },
            EntityChildren = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(DataEntity.CustomerBankingRelationship),
                    FromColumn = nameof(DataEntity.CustomerBankingRelationship.Id),
                    To = nameof(DataEntity.Contract),
                    ToColumn = nameof(DataEntity.Contract.CustomerBankingRelationshipId)
                }
            },
            ModelParents = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(CustomerBankingRelationship),
                    FromColumn = nameof(CustomerBankingRelationship.CustomerKey),
                    To = nameof(Customer),
                    ToColumn = nameof(Customer.CustomerKey)
                }
            },
            ModelChildren = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(CustomerBankingRelationship),
                    FromColumn = nameof(CustomerBankingRelationship.CustomerBankingRelationshipKey),
                    To = nameof(Contract),
                    ToColumn = nameof(Contract.CustomerBankingRelationshipKey)
                }
            },
            ModelToEntityLinks =
            {
                new LinkKey()
                {
                    From = nameof(CustomerBankingRelationship),
                    FromColumn = nameof(CustomerBankingRelationship.CustomerBankingRelationshipKey),
                    To = nameof(DataEntity.CustomerBankingRelationship),
                    ToColumn = nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey)
                }
            }
        };

        cbr.IsEntity = true;
        cbr.IsModel = true;

        cbr.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.CustomerBankingRelationship),
            nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey)));

        cbr.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(DataEntity.CustomerBankingRelationship.Id),
            DestinationEntity = nameof(DataEntity.CustomerBankingRelationship),
            DestinationName = nameof(DataEntity.CustomerBankingRelationship.Id)
        });

        cbr.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(CustomerBankingRelationship.CustomerBankingRelationshipKey),
            DestinationEntity = nameof(DataEntity.CustomerBankingRelationship),
            DestinationName = nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey)
        });

        cbr.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(CustomerBankingRelationship.CustomerKey),
            DestinationEntity = nameof(DataEntity.CustomerBankingRelationship),
            DestinationName = nameof(DataEntity.CustomerBankingRelationship.CustomerKey)
        });

        mappings.TryAdd(nameof(CustomerBankingRelationship), MappingRegistry.Register(typeof(CustomerBankingRelationship),
            typeof(DataEntity.CustomerBankingRelationship), cbr));
    }

    public void Register(Dictionary<string, NodeMap> mappings)
    {
        RegisterNodeMap(mappings);
    }
}