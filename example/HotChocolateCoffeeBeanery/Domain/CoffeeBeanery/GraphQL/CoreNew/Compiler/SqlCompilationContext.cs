namespace CoffeeBeanery.GraphQL.CoreNew.Compiler
{
    public sealed class SqlCompilationContext
    {
        public List<string> OrderByClauses { get; } = new();
        public bool HasSorting { get; set; }
        public bool HasPagination { get; set; }
        public Pagination Pagination { get; } = new();
        public Dictionary<string,string> WhereClauses {get;} = 
            new(StringComparer.OrdinalIgnoreCase);
    }
}
