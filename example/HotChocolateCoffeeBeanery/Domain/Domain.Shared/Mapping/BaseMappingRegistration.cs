using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace Domain.Shared.Mapping
{
    public abstract class BaseMappingRegistration<TModel, TEntity> : IMappingRegistration
        where TModel : class
        where TEntity : class
    {
        protected readonly string Prefix;
        protected readonly string RegistrationKey;

        protected BaseMappingRegistration(string alias)
        {
            Prefix = alias;
            // FIXED: compute directly, not via A(nameof(TModel>() which depends on Prefix
            RegistrationKey = string.IsNullOrWhiteSpace(alias)
                ? typeof(TModel).Name
                : $"{alias}{typeof(TModel).Name}";
        }

        protected string A(string name) =>
            string.IsNullOrWhiteSpace(Prefix) ? name : $"{Prefix}{name}";

        protected virtual bool     IsEntity => true;
        protected virtual bool     IsModel  => true;
        protected virtual bool     IsGraph  => false;
        protected virtual EnumMap? EnumMap  => null;

        protected abstract NodeMap BuildMap();

        public void Register()
        {
            Console.WriteLine($"[REGISTER] Prefix='{Prefix}' RegistrationKey='{RegistrationKey}'");
            
            var map      = BuildMap();
            map.IsEntity = IsEntity;
            map.IsModel  = IsModel;
            map.IsGraph  = IsGraph;
            map.Alias    = RegistrationKey;

            MappingRegistry.Register(typeof(TModel), typeof(TEntity), map, RegistrationKey);
        }
    }

    public abstract class BaseModelMappingRegistration<TModel> : IMappingRegistration
        where TModel : class
    {
        protected readonly string Prefix;
        protected readonly string RegistrationKey;

        protected BaseModelMappingRegistration(string alias)
        {
            Prefix = alias;
            // FIXED: same pattern — compute directly
            RegistrationKey = string.IsNullOrWhiteSpace(alias)
                ? typeof(TModel).Name
                : $"{alias}{typeof(TModel).Name}";
        }

        protected string A(string name) =>
            string.IsNullOrWhiteSpace(Prefix) ? name : $"{Prefix}{name}";

        protected virtual EnumMap? EnumMap => null;

        protected abstract NodeMap BuildMap();

        public void Register()
        {
            var map      = BuildMap();
            map.IsModel  = true;
            map.IsEntity = false;
            map.Alias    = RegistrationKey;

            MappingRegistry.Register(typeof(TModel), entityType: null, map, RegistrationKey);
        }
    }
}