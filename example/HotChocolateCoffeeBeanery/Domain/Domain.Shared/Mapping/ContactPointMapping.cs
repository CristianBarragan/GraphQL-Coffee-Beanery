using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using DataEntity = Database.Entity;

namespace Domain.Shared.Mapping;

public class ContactPointMapping : BaseMappingRegistration<ContactPoint, DataEntity.ContactPoint>
{
    protected override string Alias => nameof(ContactPoint);

    protected override NodeMap BuildMap()
    {
        var map = new NodeMap
        {
            Schema = nameof(DataEntity.Schema.Banking)
        };

        map.EntityParents.Add(new LinkKey
        {
            From       = nameof(DataEntity.ContactPoint),
            FromColumn = nameof(DataEntity.ContactPoint.CustomerId),
            To         = nameof(DataEntity.Customer),
            ToColumn   = nameof(DataEntity.Customer.Id)
        });

        map.ModelParents.Add(new LinkKey
        {
            From       = nameof(ContactPoint),
            FromColumn = nameof(ContactPoint.CustomerKey),
            To         = nameof(Customer),
            ToColumn   = nameof(Customer.CustomerKey)
        });

        map.ModelToEntityLinks.Add(new LinkKey
        {
            From       = nameof(ContactPoint),
            FromColumn = nameof(ContactPoint.ContactPointKey),
            To         = nameof(DataEntity.ContactPoint),
            ToColumn   = nameof(DataEntity.ContactPoint.ContactPointKey)
        });

        map.UpsertKeys.Add(new UpsertKey(
            nameof(DataEntity.ContactPoint),
            nameof(DataEntity.ContactPoint.ContactPointKey)
        ));

        map.FieldMaps.AddRange(new[]
        {
            new FieldMap { SourceName = nameof(DataEntity.ContactPoint.Id),      DestinationEntity = nameof(DataEntity.ContactPoint), DestinationName = nameof(DataEntity.ContactPoint.Id) },
            new FieldMap { SourceName = nameof(ContactPoint.ContactPointKey),     DestinationEntity = nameof(DataEntity.ContactPoint), DestinationName = nameof(DataEntity.ContactPoint.ContactPointKey) },
            new FieldMap { SourceName = nameof(ContactPoint.ContactPointValue),   DestinationEntity = nameof(DataEntity.ContactPoint), DestinationName = nameof(DataEntity.ContactPoint.ContactPointValue) },
            new FieldMap { SourceName = nameof(ContactPoint.CustomerKey),         DestinationEntity = nameof(DataEntity.ContactPoint), DestinationName = nameof(DataEntity.ContactPoint.CustomerKey) }
        });

        return map;
    }
}