using CoffeeBeanery.GraphQL.Core.Mapping;

namespace Domain.Shared.Mapping
{
    public abstract class BaseMappingRegistration<TModel, TEntity> : IMappingRegistration
        where TModel : class
        where TEntity : class
    {
        protected readonly string Prefix;
        protected readonly string Model;
        protected readonly string RegistrationKey;

        protected BaseMappingRegistration(string alias, string model)
        {
            Prefix = alias;
            RegistrationKey = string.IsNullOrWhiteSpace(alias)
                ? typeof(TModel).Name
                : $"{alias}{typeof(TModel).Name}";
            Model = model;
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
            map.ModelName = Model;

            MappingRegistry.Register(typeof(TModel), typeof(TEntity), map, RegistrationKey);
        }
    }

    public abstract class BaseModelMappingRegistration<TModel> : IMappingRegistration
        where TModel : class
    {
        protected readonly string Prefix;
        protected readonly string Model;
        protected readonly string RegistrationKey;

        protected BaseModelMappingRegistration(string alias, string model)
        {
            Prefix = alias;
            RegistrationKey = string.IsNullOrWhiteSpace(alias)
                ? typeof(TModel).Name
                : $"{alias}{typeof(TModel).Name}";
            Model = model;
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
            map.ModelName = Model;

            MappingRegistry.Register(typeof(TModel), entityType: null, map, RegistrationKey);
        }
    }
}