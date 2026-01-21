using CoffeeBeanery.GraphQL.Core.Helper;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.Service;
using Domain.Model;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;

namespace Api.Banking.Mutation;

[ExtendObjectType("WrapperMutation")]
public class WrapperMutationResolver : IInputType, IOutputType
{
    private readonly ILogger<WrapperMutationResolver> _logger;

    public WrapperMutationResolver(ILogger<WrapperMutationResolver> logger)
    {
        _logger = logger;
    }
    
    [UsePaging]
    [UseFiltering]
    [UseSorting]
    public async Task<Connection<Wrapper>> UpsertWrapper(
        [Service] IProcessService<Wrapper> service,
        [SchemaService] IResolverContext resolverContext, Wrapper wrapper)
    {
        try
        {
            var set = await service.MutationProcessAsync(
                wrapper.CacheKey, resolverContext.Selection, 
                wrapper.Model.ToString(), nameof(Wrapper), CancellationToken.None);

            var connection = ContextResolverHelper.GenerateConnection<Wrapper>(
                set.Models.Select(a => new EntityNode<Wrapper>((Wrapper)a, nameof(Wrapper))),
                new Pagination
                {
                    TotalRecordCount = new TotalRecordCount { RecordCount = set.TotalCount },
                    TotalPageRecords = new TotalPageRecords { PageRecords = set.TotalPageRecords },
                    StartCursor = set.StartCursor,
                    After = set.EndCursor?.ToString(),
                });
            
            return connection;
        }
        catch (Exception ex)
        {
            _logger.LogError($"Exception: {ex.Message} with inner exception {ex.InnerException}");
        }

        return default;
    }

    public TypeKind Kind { get; }
    public Type RuntimeType { get; }
}