using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class EntityMap<TModel, TEntity>
    {
        public List<UpsertKey> UpsertKeys { get; } = new();
        public List<FieldMap<TModel, TEntity>> FieldMaps { get; } = new();
        public List<LinkMap<TModel, TEntity>> LinkMaps { get; } = new();
        public List<EnumMapWrapper> EnumMaps { get; } = new();

        public string Schema { get; set; }
    }
}