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
            new List<KeyValuePair<string, (string, int)>>()
            {
                new($"{nameof(ContactPoint)}~{nameof(DataEntity.ContactPoint)}~{nameof(DataEntity.ContactPointType)}", (ContactPointType.Mobile.ToString(), (int)DataEntity.ContactPointType.Mobile)),
                new($"{nameof(ContactPoint)}~{nameof(DataEntity.ContactPoint)}~{nameof(DataEntity.ContactPointType)}", (ContactPointType.Landline.ToString(), (int)DataEntity.ContactPointType.Landline)),
                new($"{nameof(ContactPoint)}~{nameof(DataEntity.ContactPoint)}~{nameof(DataEntity.ContactPointType)}", (ContactPointType.Email.ToString(), (int)DataEntity.ContactPointType.Email))
            },
            new List<KeyValuePair<string, (string, int)>>()
            {
                new($"{nameof(DataEntity.ContactPoint)}~{nameof(ContactPoint)}~{nameof(DataEntity.ContactPointType)}", (DataEntity.ContactPointType.Mobile.ToString(), (int)ContactPointType.Mobile)),
                new($"{nameof(DataEntity.ContactPoint)}~{nameof(ContactPoint)}~{nameof(DataEntity.ContactPointType)}", (DataEntity.ContactPointType.Landline.ToString(), (int)ContactPointType.Landline)),
                new($"{nameof(DataEntity.ContactPoint)}~{nameof(ContactPoint)}~{nameof(DataEntity.ContactPointType)}", (DataEntity.ContactPointType.Email.ToString(), (int)ContactPointType.Email))
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