using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.Helper;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;

namespace Api.Banking.Mutation;

[ExtendObjectType("WrapperMutation")]
public class WrapperMutationResolver
{
    private readonly ILogger<WrapperMutationResolver> _logger;

    public WrapperMutationResolver(
        ILogger<WrapperMutationResolver> logger)
    {
        _logger = logger;
    }

    [UsePaging]
    [UseFiltering]
    public async Task<Connection<Wrapper>> UpsertWrapper(
        [Service] IProcessService<Wrapper> service,
        IResolverContext resolverContext,
        Wrapper wrapper)
    {
        try
        {
            var set = await service.MutationProcessAsync(
                wrapper.CacheKey,
                resolverContext.Selection,
                CancellationToken.None);

            return ContextResolverHelper.ToConnection(
                set.Items,
                new Pagination(),
                totalCount: set.Items.Count,
                cursorSelector: x => x.CacheKey.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "UpsertWrapper failed: {Message}",
                ex.Message);

            throw;
        }
    }
}