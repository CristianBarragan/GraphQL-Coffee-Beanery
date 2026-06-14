namespace CoffeeBeanery.GraphQL.Core.Runtime;

public class PaginationPlan
{
    public int First { get; set; }
    public int Last { get; set; }

    public string? After { get; set; }
    public string? Before { get; set; }

    public int? StartCursor { get; set; }
    public int? EndCursor { get; set; }

    public int? TotalRecordCount { get; set; }
    public int? TotalPageRecords { get; set; }

    public int PageSize => First > 0 ? First : Last;
}