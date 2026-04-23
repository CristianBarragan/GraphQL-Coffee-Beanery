using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.Service;
using Domain.Model;
// using Domain.Shared.Query;
using FASTER.core;
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

            services = AddCache(services);

            // services.AddScoped<IProcessService<dynamic>, ProcessService<dynamic>>();
            // services.AddScoped<IQuery<ProcessQueryParameters,
            //         (List<dynamic> list, int? startCursor, int? endCursor, int? totalCount, int?
            //         totalPageRecords)>,
            //     CustomerCustomerEdgeQueryHandler<dynamic>>();

            services.AddScoped<IProcessService<Wrapper>, ProcessService<Wrapper>>();
            // services.AddScoped<IQuery<ProcessQueryParameters,
            //         (List<CustomerCustomerEdge> list, int? startCursor, int? endCursor, int? totalCount, int?
            //         totalPageRecords)>,
            //     CustomerCustomerEdgeQueryHandler<CustomerCustomerEdge>>();
            
            services.AddScoped<
                IQuery<ProcessQueryParameters,
                    (List<Wrapper>, int?, int?, int?, int?)>,
                ProcessQuery<Wrapper>
            >();

            // services.AddScoped<IQuery<ProcessQueryParameters,
            //         (List<CustomerCustomerEdge> list, int? startCursor, int? endCursor, int? totalCount,
            //         int? totalPageRecords)>,
            //     CustomerCustomerEdgeQueryHandler<CustomerCustomerEdge>>();

            services.AddScoped<IQueryDispatcher, QueryDispatcher>();
            services.AddScoped<UnitOfWork, UnitOfWork>();
            services.AddScoped<IUnitOfWorkContext, UnitOfWorkContext>();

            return services;
        }

        private static IServiceCollection AddCache(this IServiceCollection services)
        {
            var store = new FasterKV<string, string>(128,
                new LogSettings
                {
                    LogDevice = Devices.CreateLogDevice("C:/database"),
                    ObjectLogDevice = new ManagedLocalStorageDevice("C:/database")
                });
            store.TakeHybridLogCheckpointAsync(CheckpointType.FoldOver);
            services.AddSingleton<IFasterKV<string, string>>(store);
            return services;
        }
    }
}