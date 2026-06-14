using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Types.Pagination;

namespace CoffeeBeanery.GraphQL.Core.Helper;

/// <summary>
/// GraphQL-layer only helper: builds Connection<T> from hydrated results.
/// NO SQL awareness, NO EntityNode, NO cursor generation from row index.
/// </summary>
public static class ContextResolverHelper
{
    public static Connection<T> ToConnection<T>(
        IList<T> items,
        Pagination pagination,
        int? totalCount = null,
        Func<T, string>? cursorSelector = null)
        where T : class
    {
        cursorSelector ??= (_ => string.Empty);

        var edges = new List<Edge<T>>(items.Count);

        for (var i = 0; i < items.Count; i++)
        {
            var entity = items[i];

            var cursor = cursorSelector(entity);
            edges.Add(new Edge<T>(entity, cursor));
        }

        var hasPreviousPage =
            pagination.After is not null ||
            pagination.Before is not null ||
            pagination.First > 0;

        var hasNextPage =
            totalCount.HasValue &&
            pagination.First > 0 &&
            items.Count >= pagination.First;

        var pageInfo = new ConnectionPageInfo(
            hasNextPage,
            hasPreviousPage,
            pagination.StartCursor?.ToString(),
            pagination.EndCursor?.ToString());

        return new Connection<T>(
            edges,
            pageInfo,
            totalCount ?? 0);
    }
}