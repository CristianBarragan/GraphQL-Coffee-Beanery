using CoffeeBeanery.CQRS;
using CoffeeBeanery.Service;
using Domain.Model;
using Domain.Shared.Query;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Domain.Shared.Extension
{
    public static class ModelServiceCollectionRegistration
    {
        public static IServiceCollection AddBankingDomainModelServiceCollection(
            this IServiceCollection services,
            string postgresConnectionString)
        {
            services.AddSingleton(sp =>
                new NpgsqlConnection(postgresConnectionString));

            services.AddScoped(typeof(IDynamicQueryHandler), typeof(DynamicQueryHandler));
            services.AddScoped(typeof(IProcessService<>), typeof(ProcessService<>));
            
            // services.AddScoped(typeof(ProcessQuery<>));
            services.AddScoped<DynamicQueryHandler>();
            services.AddScoped<CustomerCustomerEdgeQueryHandler>();

            // CQRS
            services.AddScoped<IQueryDispatcher, QueryDispatcher>();

            return services;
        }
    }
}