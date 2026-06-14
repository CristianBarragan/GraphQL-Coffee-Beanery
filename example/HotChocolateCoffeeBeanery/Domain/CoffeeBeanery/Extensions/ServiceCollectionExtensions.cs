using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.GraphQL.Engine;
using CoffeeBeanery.Service;

namespace CoffeeBeanery.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddCoffeeBeaneryQueryEngine(
        this IServiceCollection services)
    {
        services.AddScoped<IQueryOptimizer, QueryOptimizer>();
        services.AddScoped<IGraphEngine, GraphEngine>();
        services.AddScoped<IQueryEngine, QueryEngine>();
        services.AddScoped<IMutationEngine, MutationEngine>();
        services.AddScoped<IQueryExecutor, QueryExecutor>();
        services.AddScoped<IHydrationEngine, HydrationEngine>();
        services.AddScoped<IQueryPlanner, QueryPlanner>();
        services.AddScoped<IGraphILCache, GraphILCache>();
        services.AddSingleton<ExecutionCache>();
        services.AddSingleton<QueryTraceCollector>();
        services.AddScoped(typeof(QueryHandler<>));
        services.AddScoped(typeof(IProcessService<>), typeof(ProcessService<>));

        return services;
    }
}