using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public sealed class SqlCompilationContext
    {
        public string SelectSql { get; set; } = "";
        public string UpsertSql { get; set; } = "";

        public string SqlWhereStatement { get; set; } = "";
        
        public string SqlOrderStatement { get; set; } = "";

        public Pagination Pagination { get; set; } = new Pagination();

        public List<string> SelectSqlFields { get; } = new();
    }
}