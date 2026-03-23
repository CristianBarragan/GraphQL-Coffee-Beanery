using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;

namespace Domain.Shared.Mapping;
using DataEntity = Database.Entity;

public class ContactPointMapping : IMappingRegistration
{
    public void RegisterNodeMap(Dictionary<string, NodeMap> mappings)
    {
        var cp = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Banking)
        };

        cp.IsEntity = true;
        cp.IsModel = true;

        cp.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.ContactPoint),
            nameof(DataEntity.ContactPoint.ContactPointKey)));

        cp.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(DataEntity.ContactPoint.Id),
            DestinationEntity = nameof(DataEntity.ContactPoint),
            DestinationName = nameof(DataEntity.ContactPoint.Id)
        });

        cp.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(ContactPoint.ContactPointKey),
            DestinationEntity = nameof(DataEntity.ContactPoint),
            DestinationName = nameof(DataEntity.ContactPoint.ContactPointKey)
        });

        cp.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(ContactPoint.ContactPointValue),
            DestinationEntity = nameof(DataEntity.ContactPoint),
            DestinationName = nameof(DataEntity.ContactPoint.ContactPointValue)
        });

        cp.FieldMaps.Add(new FieldMap
        {
            SourceName = nameof(ContactPoint.CustomerKey),
            DestinationEntity = nameof(DataEntity.ContactPoint),
            DestinationName = nameof(DataEntity.ContactPoint.CustomerKey)
        });

        // Enum mapping for ContactPointType
        var cpEnums = EnumMapFactory.Create(
            new Dictionary<string, (ContactPointType, DataEntity.ContactPointType)>
            {
                { $"{nameof(ContactPoint)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContactPointType.Mobile}", (ContactPointType.Mobile, DataEntity.ContactPointType.Mobile) },
                { $"{nameof(ContactPoint)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContactPointType.Landline}", (ContactPointType.Landline, DataEntity.ContactPointType.Landline) },
                { $"{nameof(ContactPoint)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContactPointType.Email}", (ContactPointType.Email, DataEntity.ContactPointType.Email) }
            },
            new Dictionary<string, (DataEntity.ContactPointType, ContactPointType)>
            {
                { $"{nameof(DataEntity.ContactPoint)}~{nameof(ContactPoint)}~{ContactPointType.Mobile}", (DataEntity.ContactPointType.Mobile, ContactPointType.Mobile) },
                { $"{nameof(DataEntity.ContactPoint)}~{nameof(ContactPoint)}~{ContactPointType.Landline}", (DataEntity.ContactPointType.Landline, ContactPointType.Landline) },
                { $"{nameof(DataEntity.ContactPoint)}~{nameof(ContactPoint)}~{ContactPointType.Email}", (DataEntity.ContactPointType.Email, ContactPointType.Email) }
            });

        cp.FromEnum = cpEnums.from;
        cp.ToEnum = cpEnums.to;

        mappings.TryAdd(nameof(ContactPoint), MappingRegistry.Register(typeof(ContactPoint), typeof(DataEntity.ContactPoint), cp));
    }

    public void Register(Dictionary<string, NodeMap> mappings)
    {
        RegisterNodeMap(mappings);
    }
}