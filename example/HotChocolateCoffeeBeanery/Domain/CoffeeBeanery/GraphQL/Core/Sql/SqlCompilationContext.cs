using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public sealed class SqlCompilationContext
    {
        public List<string> OrderClauses { get; } = new();
        public bool HasSorting => OrderClauses.Count > 0;

        public Pagination Pagination { get; } = new();
        public bool HasPagination { get; set; }

        private readonly Dictionary<string, string> _where = new();
        public bool TryGetWhere(string root, out string where) => _where.TryGetValue(root, out where);
        public void AddWhere(string root, string clause) => _where[root] = clause;

        public void AddOrder(string clause) => OrderClauses.Add(clause);
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