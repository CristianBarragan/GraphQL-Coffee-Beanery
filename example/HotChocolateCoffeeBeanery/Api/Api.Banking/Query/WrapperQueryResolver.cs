using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.Helper;
using CoffeeBeanery.Service;
using Domain.Model;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace Api.Banking.Query;

[ExtendObjectType("WrapperQuery")]
public class WrapperQueryResolver
{
    private readonly ILogger<WrapperQueryResolver> _logger;

    public WrapperQueryResolver(
        ILogger<WrapperQueryResolver> logger)
    {
        _logger = logger;
    }

    [UsePaging]
    [UseFiltering]
    public async Task<Connection<Wrapper>> GetWrapper(
        [Service] IProcessService<Wrapper> service,
        [SchemaService] IResolverContext resolverContext,
        Wrapper wrapper)
    {
        try
        {
            var cacheKey = string.Empty;
            
            var set = await service.QueryProcessAsync(
                wrapper.CacheKey, resolverContext.Selection,
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
                $"Exception: {ex.Message} with inner exception {ex.InnerException}");
        }

        return default!;
    }
}
