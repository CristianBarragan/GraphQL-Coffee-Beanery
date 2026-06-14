using CoffeeBeanery.CQRS;
using CoffeeBeanery.Extensions;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.Service;
using Domain.Model;
using Domain.Shared.Mapping;

namespace Domain.Shared.Extension
{
    public static class ModelServiceCollectionRegistration
    {
        public static IServiceCollection AddCoffeeBeanery(
            this IServiceCollection services)
        {
            services.AddScoped<CoffeeBeanery.GraphQL.Core.Contracts.IQuery<ProcessQueryParameters, List<object[]>>,
                ProcessQuery<Wrapper>>();

            services.Init<IMappingSet<CustomerMappingType, Model.Model>, CustomerMappingType, Domain.Model.Model>(
                typeof(ModelMappingRegistration).Assembly);
            
            SqlNodeBuilder.Build();
        
            services.AddCoffeeBeaneryQueryEngine();

            services.AddScoped<IProcessService<Wrapper>, ProcessService<Wrapper>>();
            services.AddScoped<UnitOfWork, UnitOfWork>();
            services.AddScoped<IUnitOfWorkContext, UnitOfWorkContext>();

            return services;
        }
    }
}