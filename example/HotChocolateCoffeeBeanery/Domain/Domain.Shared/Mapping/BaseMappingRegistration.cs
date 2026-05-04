using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace Domain.Shared.Mapping
{
    public abstract class BaseMappingRegistration<TModel, TEntity> : IMappingRegistration
        where TModel : class
        where TEntity : class
    {
        private string? _alias;

        protected BaseMappingRegistration(string alias)
        {
            _alias = alias;
        }

        protected virtual string? Alias
        {
            get => _alias;
            set => _alias = value;
        }

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
        private string? _alias;
        
        public BaseModelMappingRegistration(string alias)
        {
            _alias = $"{alias}{typeof(TModel).Name}";
        }

        protected virtual string? Alias
        {
            get => _alias;
            set => _alias = value;
        }
        
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