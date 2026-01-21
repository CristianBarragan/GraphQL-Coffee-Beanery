using System;

namespace CoffeeBeanery.GraphQL.Core.Mapping
{
    public static class MappingBuilder
    {
        public static ModelMapping<TModel> Create<TModel>(
            Action<ModelMapping<TModel>> configure)
            where TModel : class
        {
            var map = new ModelMapping<TModel>();
            configure(map);
            return map;
        }
    }
}