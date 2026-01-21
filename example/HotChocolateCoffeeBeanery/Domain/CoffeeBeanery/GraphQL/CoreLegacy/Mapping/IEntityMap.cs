using System;
using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public interface IEntityMap
    {
        Type EntityType { get; }
        IReadOnlyList<FieldMap> FieldMaps { get; }
        IReadOnlyList<LinkMap> LinkMaps { get; }
        IReadOnlyList<UpsertKey> UpsertKeys { get; }
        IReadOnlyList<IEnumMap> EnumMaps { get; }
    }
    
    public class EntityMap<TModel, TEntity> : IEntityMap
        where TModel : class
        where TEntity : class
    {
        public Type EntityType => typeof(TEntity);

        public List<FieldMap> FieldMaps { get; } = new();
        public List<LinkMap> LinkMaps { get; } = new();
        public List<UpsertKey> UpsertKeys { get; } = new();
        public List<IEnumMap> EnumMaps { get; } = new();

        IReadOnlyList<FieldMap> IEntityMap.FieldMaps => FieldMaps;
        IReadOnlyList<LinkMap> IEntityMap.LinkMaps => LinkMaps;
        IReadOnlyList<UpsertKey> IEntityMap.UpsertKeys => UpsertKeys;
        IReadOnlyList<IEnumMap> IEntityMap.EnumMaps => EnumMaps;
    }
}