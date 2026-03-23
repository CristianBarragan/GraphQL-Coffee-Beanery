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
            Schema = nameof(DataEntity.Schema.Banking)
        };

        cbr.IsEntity = true;

        cbr.Children.Add(nameof(DataEntity.Contract));

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