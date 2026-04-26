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
            Schema = nameof(DataEntity.Schema.Banking),
            EntityParents = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(DataEntity.ContactPoint),
                    FromColumn = nameof(DataEntity.ContactPoint.CustomerId),
                    To = nameof(DataEntity.Customer),
                    ToColumn = nameof(DataEntity.Customer.Id)
                }
            },
            ModelParents = new List<LinkKey>()
            {
                new LinkKey()
                {
                    From = nameof(ContactPoint),
                    FromColumn = nameof(ContactPoint.CustomerKey),
                    To = nameof(Customer),
                    ToColumn = nameof(Customer.CustomerKey)
                }
            },
            ModelToEntityLinks =
            {
                new LinkKey()
                {
                    From = nameof(ContactPoint),
                    FromColumn = nameof(ContactPoint.ContactPointKey),
                    To = nameof(DataEntity.ContactPoint),
                    ToColumn = nameof(DataEntity.ContactPoint.ContactPointKey)
                }
            }
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
            new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
            {
                { $"{nameof(ContactPoint)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContactPointType.Mobile}", (ContactPointType.Mobile.ToString(), (int)DataEntity.ContactPointType.Mobile) },
                { $"{nameof(ContactPoint)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContactPointType.Landline}", (ContactPointType.Landline.ToString(), (int)DataEntity.ContactPointType.Landline) },
                { $"{nameof(ContactPoint)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContactPointType.Email}", (ContactPointType.Email.ToString(), (int)DataEntity.ContactPointType.Email) }
            },
            new Dictionary<string, (string, int)>(StringComparer.OrdinalIgnoreCase)
            {
                { $"{nameof(DataEntity.ContactPoint)}~{nameof(ContactPoint)}~{ContactPointType.Mobile}", (DataEntity.ContactPointType.Mobile.ToString(), (int)ContactPointType.Mobile) },
                { $"{nameof(DataEntity.ContactPoint)}~{nameof(ContactPoint)}~{ContactPointType.Landline}", (DataEntity.ContactPointType.Landline.ToString(), (int)ContactPointType.Landline) },
                { $"{nameof(DataEntity.ContactPoint)}~{nameof(ContactPoint)}~{ContactPointType.Email}", (DataEntity.ContactPointType.Email.ToString(), (int)ContactPointType.Email) }
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