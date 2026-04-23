using System;
using System.Threading;
using System.Threading.Tasks;
using CoffeeBeanery.GraphQL.Core.Helper;
using CoffeeBeanery.Service;
using Domain.Model;
using HotChocolate.Resolvers;
using HotChocolate.Types.Pagination;
using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.Extensions.Logging;

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
    [UseSorting]
    public async Task<Connection<Wrapper>> GetWrapper(
        [Service] IProcessService<Wrapper> service,
        [SchemaService] IResolverContext resolverContext,
        CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = string.Empty;
            
            var set = await service.QueryProcessAsync(
                cacheKey,
                resolverContext.Selection,
                nameof(Customer),
                nameof(Wrapper),
                cancellationToken);

            var entityNodes = set.Models
                .Where(a => a is not null)                // guard against null rows
                .Select(a => new EntityNode<Wrapper>(a, nameof(Wrapper)));

            var connection = ContextResolverHelper.GenerateConnection<Wrapper>(
                entityNodes,
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
            _logger.LogError(
                $"Exception: {ex.Message} with inner exception {ex.InnerException}");
        }

        return default!;
    }
}
