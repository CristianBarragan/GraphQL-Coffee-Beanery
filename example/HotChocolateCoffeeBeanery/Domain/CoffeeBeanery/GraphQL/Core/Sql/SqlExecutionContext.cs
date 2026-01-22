using System;
using System.Collections.Generic;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public class SqlExecutionContext
    {
        public string SqlQuery { get; set; } = "";
        public Dictionary<string, Type> SplitOnTypes { get; set; } = new();
        public Dictionary<string, string> SplitOnDapper { get; set; } = new();

        public string StartCursor { get; set; } = "";
        public string EndCursor { get; set; } = "";
        public int? TotalCount { get; set; }
        public int? TotalPages { get; set; }
    }
}