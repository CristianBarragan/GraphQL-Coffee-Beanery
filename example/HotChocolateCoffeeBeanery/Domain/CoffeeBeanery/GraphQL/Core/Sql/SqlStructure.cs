using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.GraphQL;
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

        public DynamicParameters Parameters { get; set; }

        // public OrderedDictionary<string, Type> SplitOnDapper { get; set; } = new();
        public Dictionary<string, Type> SplitOnDapper { get; set; } = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);

        public SqlNode[] SqlNodes { get; set; }

        public Dictionary<string, NodeTree> EntityTrees { get; set; } = new Dictionary<string, NodeTree>(StringComparer.InvariantCultureIgnoreCase);
        
        public Dictionary<string, NodeTree> ModelTrees { get; set; } = new Dictionary<string, NodeTree>(StringComparer.InvariantCultureIgnoreCase);

        public Dictionary<string, Type> EntityMapping { get; set; } = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);
        
        public List<Type> ModelMapping { get; set; }

        public List<string> Aliases { get; set; }
    }
}