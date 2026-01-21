namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public sealed record SqlQueryPlan(
        string Sql,
        bool HasPagination,
        bool HasSorting
    );
}
