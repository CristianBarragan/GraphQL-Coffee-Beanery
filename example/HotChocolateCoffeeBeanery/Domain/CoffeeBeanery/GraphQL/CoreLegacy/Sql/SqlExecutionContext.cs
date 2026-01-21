namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public class SqlExecutionContext
    {
        public string SqlQuery { get; set; }
        public Dictionary<string, Type> SplitOnTypes { get; set; } = new();
        public Dictionary<string, string> SplitOnDapper { get; set; } = new();

        // New properties
        public string StartCursor { get; set; }
        public string EndCursor   { get; set; }
        public int? TotalCount    { get; set; }
        public int? TotalPages    { get; set; }
    }

}
