namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public class SqlStructure
    {
        public string SqlUpsert { get; set; }
        public string SqlQuery { get; set; }
        public IReadOnlyList<string> EntityOrder { get; init; }
        public IReadOnlyDictionary<string, Type> SplitOn { get; init; }
        public bool HasTotalCount { get; set; }
        public bool HasPagination { get; set; }
        public bool HasSorting { get; set; }
        public int StartCursor { get; set; }

        public int EndCursor { get; set; }
        public Pagination Pagination { get; set; }
    }

}