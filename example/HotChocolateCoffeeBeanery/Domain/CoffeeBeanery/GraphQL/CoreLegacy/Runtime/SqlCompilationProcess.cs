using System.Collections.Generic;
using Dapper;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public sealed class SqlCompilationContext
    {
        // SELECT SQL
        public string SelectSql { get; set; } = string.Empty;
        public string OrderBy { get; set; } = string.Empty;
        public Pagination Pagination { get; set; } = new();
        public bool HasPagination { get; set; }
        public bool HasSorting { get; set; }

        // UPSERT / MUTATION SQL
        public string UpsertSql { get; set; } = string.Empty;
        public Dictionary<string, SqlNode> UpsertNodes { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        // Shared where fragments
        public Dictionary<string, string> Where { get; } =
            new(StringComparer.OrdinalIgnoreCase);

        // For caching compiled plans
        public List<string> VisitedModels { get; } = new();

        // Execution parameters
        public DynamicParameters Parameters { get; set; } = new();
    }
}