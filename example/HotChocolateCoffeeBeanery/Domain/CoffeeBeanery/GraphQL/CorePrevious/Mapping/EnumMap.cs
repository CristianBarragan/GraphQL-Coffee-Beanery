using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class EnumMap<TModel, TEntity>
    {
        public Dictionary<TModel, TEntity> ToEntity { get; set; } = new();
        public Dictionary<TEntity, TModel> ToModel { get; set; } = new();
    }
}