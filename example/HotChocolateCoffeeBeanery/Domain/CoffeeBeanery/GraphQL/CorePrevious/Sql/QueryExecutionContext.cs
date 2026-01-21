using System;
using System.Collections.Generic;
using Dapper;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public class QueryExecutionContext
    {
        public string SqlQuery { get; set; } = string.Empty;
        public string SqlUpsert { get; set; } = string.Empty;

        // Make these settable so you can assign values in the factory
        public Dictionary<string, string> SplitOnDapper { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public Dictionary<string, Type> SplitOnTypes { get; set; }
            = new(StringComparer.OrdinalIgnoreCase);

        public DynamicParameters Parameters { get; set; } = new();
        public bool HasTotalCount { get; set; }
    }
}