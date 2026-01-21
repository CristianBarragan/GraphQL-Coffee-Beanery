using System;
using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public sealed class ModelMap<TModel, TEntity>
    {
        public Type ModelType { get; }
        public List<EntityMap<TModel, TEntity>> EntityMaps { get; } = new();

        public ModelMap(Type modelType) => ModelType = modelType;
    }
}