using System.Collections.Generic;
using Dapper;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public class SqlStructure
    {
        public Pagination? Pagination { get; set; }
        public bool HasTotalCount { get; set; } = false;
        public bool HasPagination { get; set; } = false;

        public string SqlUpsert { get; set; } = "";
        public string SqlQuery { get; set; } = "";

        public Dictionary<string, Type> SplitOnDapper { get; set; } = new();
        public DynamicParameters Parameters { get; set; } = new();
    }
}