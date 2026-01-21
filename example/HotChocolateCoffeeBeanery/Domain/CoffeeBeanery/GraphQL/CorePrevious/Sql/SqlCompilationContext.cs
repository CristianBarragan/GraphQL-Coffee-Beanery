using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public sealed class SqlCompilationContext
    {
        public Dictionary<string, string> Where { get; } = new();
        public List<string> OrderClauses { get; } = new();

        public Pagination Pagination { get; } = new();
        public bool HasPagination { get; set; }
        public bool HasSorting => OrderClauses.Count > 0;

        public void AddWhere(string entity, string clause)
        {
            Where[entity] = clause;
        }

        public bool TryGetWhere(string entity, out string clause)
            => Where.TryGetValue(entity, out clause);
    }
    
    public class Pagination
    {
        public string? After { get; set; }

        public string? Before { get; set; }

        public int? First { get; set; }

        public int? Last { get; set; }

        public int? StartCursor { get; set; }

        public int? EndCursor { get; set; }

        public int PageSize { get; set; }

        public TotalRecordCount TotalRecordCount { get; set; }

        public TotalPageRecords TotalPageRecords { get; set; }
    }

    public class TotalRecordCount
    {
        public int RecordCount { get; set; }
    }

    public class TotalPageRecords
    {
        public int PageRecords { get; set; }
    }
}