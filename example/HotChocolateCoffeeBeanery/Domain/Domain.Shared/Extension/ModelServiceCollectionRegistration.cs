using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.Service;
using Domain.Model;
using Domain.Shared.Mapping;
using FASTER.core;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Domain.Shared.Extension
{
    public static class ModelServiceCollectionRegistration
    {
        public static IServiceCollection AddCoffeeBeanery<TContext>(
            this IServiceCollection services,
            string postgresConnectionString)
            where TContext : DbContext
        {
            services.AddScoped<EfEntityMetadata<TContext>>();

            services.AddSingleton(sp =>
                new NpgsqlConnection(postgresConnectionString));

            services = AddCache(services);

            // services.AddScoped<
            //     IQuery<
            //         ProcessQueryParameters,
            //         (List<Wrapper>, int?, int?, int?, int?)>,
            //     QueryHandler<Wrapper>>();

            services.Init<
                IMappingSet>(
                typeof(ModelMappingRegistration).Assembly);

            services.AddScoped<NodeBuilder<TContext>>();

            services.AddScoped<
                IProcessService<Wrapper>,
                ProcessService<Wrapper>>();

            services.AddScoped<IQueryDispatcher, QueryDispatcher>();
            services.AddScoped<UnitOfWork, UnitOfWork>();
            services.AddScoped<IUnitOfWorkContext, UnitOfWorkContext>();
            
            using (var tempScope = services.BuildServiceProvider().CreateScope())
            {
                tempScope.ServiceProvider
                    .GetRequiredService<NodeBuilder<TContext>>()
                    .BuildFromMappings();
            }

            return services;
        }

        private static IServiceCollection AddCache(
            this IServiceCollection services)
        {
            var store = new FasterKV<string, string>(
                128,
                new LogSettings
                {
                    LogDevice =
                        Devices.CreateLogDevice("C:/database"),
                    ObjectLogDevice =
                        new ManagedLocalStorageDevice("C:/database")
                });

            store.TakeHybridLogCheckpointAsync(
                CheckpointType.FoldOver);

            services.AddSingleton<
                IFasterKV<string, string>>(store);

            return services;
        }
    }
}