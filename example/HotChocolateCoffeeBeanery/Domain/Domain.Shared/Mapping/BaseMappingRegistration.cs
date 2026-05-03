using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace Domain.Shared.Mapping
{
    public abstract class BaseMappingRegistration<TModel, TEntity> : IMappingRegistration
        where TModel : class
        where TEntity : class
    {
        protected virtual string? Alias    => null;
        protected virtual bool    IsEntity => true;
        protected virtual bool    IsModel  => true;
        protected virtual bool    IsGraph  => false;
        
        protected virtual EnumMap? EnumMap => null;

        protected abstract NodeMap BuildMap();

        public void Register()
        {
            var map      = BuildMap();
            map.IsEntity = IsEntity;
            map.IsModel  = IsModel;
            map.IsGraph  = IsGraph;

            if (Alias != null)
                map.Alias = Alias;

            MappingRegistry.Register(typeof(TModel), typeof(TEntity), map, Alias);
        }
    }

    public abstract class BaseModelMappingRegistration<TModel> : IMappingRegistration
        where TModel : class
    {
        protected virtual string? Alias => null;
        
        protected virtual EnumMap? EnumMap => null;
        
        protected abstract NodeMap BuildMap();

        public void Register()
        {
            var map     = BuildMap();
            map.IsModel = true;
            map.IsEntity = false;
            
            if (Alias != null)
                map.Alias = Alias;

            MappingRegistry.Register(typeof(TModel), entityType: null, map, Alias);
        }
    }
}