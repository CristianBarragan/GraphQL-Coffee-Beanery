using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class EntityMap<TModel, TEntity>
        where TModel : class
        where TEntity : class
    {
        public string Schema { get; set; } = "public";

        public List<UpsertKey> UpsertKeys { get; init; } = new();
        public List<FieldMap<TModel, TEntity>> FieldMaps { get; init; } = new();
        public List<LinkMap<TModel, TEntity>> LinkMaps { get; init; } = new();
        public List<object> EnumMaps { get; init; } = new();
    }
}