using System;

namespace CoffeeBeanery.GraphQL.Core.Execution
{
    public class GraphExecutor
    {
        public TResult Execute<TModel, TResult>(TModel model, TResult entity)
            where TModel : class
            where TResult : class
        {
            var map = MappingRegistry.Get<TModel>();
            if (!map.EntityMaps.TryGetValue(typeof(TResult), out var imap))
                throw new InvalidOperationException($"No mapping for {typeof(TResult).Name}");

            dynamic emap = imap;
            foreach (var fm in emap.FieldMaps)
            {
                var mval = ((dynamic)fm.Source).Compile()(model);
                ((dynamic)fm.Destination).Compile()(entity, mval);
            }

            return entity;
        }
    }
}