using System.Collections.Generic;

namespace CoffeeBeanery.Service
{
    public sealed class QueryResult
    {
        public IEnumerable<object> Models { get; set; } = new List<object>();
        public int? StartCursor { get; set; }
        public int? EndCursor { get; set; }
        public int TotalCount { get; set; }
        public int TotalPageRecords { get; set; }
    }
}