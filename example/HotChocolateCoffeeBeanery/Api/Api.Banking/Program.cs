using Amazon;
using Amazon.RDS.Util;
using Api.Banking.Mutation;
using Api.Banking.Query;
using CoffeeBeanery.GraphQL.Core.Warmup;
using Domain.Model;
using Domain.Shared.Extension;
using DataEntity = Database.Entity;
using HotChocolate.AspNetCore;
using HotChocolate.Types.Pagination;

namespace Api.Banking;

public class Program
{
    public static void Main(string[] args)
    {
        var app = CreateHostBuilder(args);
        app.UseWebSockets();
        app.UseRouting();
        app.UseHttpsRedirection();
        app.UseEndpoints(endpoints => { endpoints.MapControllers(); });
        app.MapGraphQL();
        app.MapNitroApp("/graphql-ui/").WithOptions(new GraphQLToolOptions()
            { ServeMode = GraphQLToolServeMode.Embedded });
        app.MapControllers();

        app.Run();
    }

    public static WebApplication CreateHostBuilder(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        var services = builder.Services;

        IConfiguration configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true).AddEnvironmentVariables()
            .AddCommandLine(args).Build();

        var connectionString = configuration.GetConnectionString("BankingConnectionString");

        services.AddBankingDomainModelServiceCollection(connectionString);
        
        // var modelNodes = new Dictionary<string, SqlNode>();
        // var entityNodes = new Dictionary<string, SqlNode>();
        //
        // MappingSetup.RegisterAll();
        // MappingSetup.BuildAll(modelNodes, entityNodes);
        //
        // var modelTree = NodeTreeBuilder.Build(modelNodes);
        // var entityTree = NodeTreeBuilder.Build(entityNodes);

        // MappingSetup.RegisterAll();
        //
        // var mapper = BulkMapper.Compile<Domain.Model.Customer, Database.Entity.Customer>(
        //     MappingRegistry.TryGet<Domain.Model.Customer, Database.Entity.Customer>(out var mappings)
        //         ? (PropertyMapping<Domain.Model.Customer, Database.Entity.Customer>[])mappings
        //         : throw new Exception("Not registered"));
        //
        // var model = new Domain.Model.Customer { CustomerKey = Guid.NewGuid(), FirstNaming = "John" };
        // var entity = new Database.Entity.Customer();
        // mapper(model, entity);
        
        // BulkMapper.Compile(AppDomain.CurrentDomain.GetAssemblies());

        // MappingRegistry.Register(
        //     Shared.Mapping.Mappings.ModelToEntity()
        // );

        // var executor = new GraphExecutor();
        //
        // var model = new Customer { FirstNaming = "A" };
        // var entity = new Database.Entity.Customer();
        //
        // executor.ExecuteMap(model, entity);

        
        GraphWarmup.Init(
            typeof(Domain.Model.Customer).Assembly
        );
        //
        //
        // MappingSetup.RegisterAll();
        // MappingSetup.BuildAll(modelNodes, entityNodes);
        //
        // var modelTree = NodeTreeBuilder.Build(modelNodes);
        // var entityTree = NodeTreeBuilder.Build(entityNodes);
        //
        // MappingSetup.RegisterAll();
        //
        // var runtime = GraphRuntimeBuilder.Build();
        // var executor = new GraphExecutor(runtime);
        //
        // var model = new Domain.Model.Customer { CustomerKey= Guid.NewGuid(), FirstNaming="A", LastNaming="B" };
        // var entity = new Database.Entity.Customer();
        //
        // executor.ExecuteMap("ignored", model, entity);


        
        var isRds = false;

        if (isRds)
        {
            services.AddNpgsqlDataSource(connectionString!, dataSourceBuilder =>
            {
                dataSourceBuilder.UsePeriodicPasswordProvider(async (settings, cancellationToken) =>
                    {
                        return await Task.Run(
                            () => RDSAuthTokenGenerator.GenerateAuthToken(RegionEndpoint.APSoutheast2, settings.Host,
                                settings.Port,
                                settings.Username), cancellationToken);
                    }, TimeSpan.FromSeconds(10),
                    TimeSpan.FromSeconds(10));
            });
        }
        else
        {
            builder.Services.AddNpgsqlDataSource(connectionString!);
        }

        builder.Services.AddControllers().AddNewtonsoftJson();

        builder.Services.AddGraphQLServer()
            .AddQueryType(d =>
            {
                d.Field("wrapper")
                    .ResolveWith<WrapperQueryResolver>(r => r.GetWrapper(default, default,
                        default));
            })
            .AddMutationType(d =>
            {
                d.Name("Mutation");

                d.Field("wrapper")
                    .Argument("wrapper", d => d.Type<CustomerInputType>())
                    .ResolveWith<WrapperMutationResolver>(r => r.UpsertWrapper(default, default, default));
            })
            .SetPagingOptions(new PagingOptions() { DefaultPageSize = 10, IncludeTotalCount = true })
            .AddFiltering()
            .AddSorting()
            .InitializeOnStartup();

        Dapper.DefaultTypeMap.MatchNamesWithUnderscores = true;
        return builder.Build();
    }
}