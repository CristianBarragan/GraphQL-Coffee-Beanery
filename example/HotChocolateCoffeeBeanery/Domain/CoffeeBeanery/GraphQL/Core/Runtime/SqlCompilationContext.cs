using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    public sealed class SqlCompilationContext
    {
        public string SelectSql { get; set; } = "";
        public string UpsertSql { get; set; } = "";

        public bool HasPagination { get; set; }
        
        public bool HasTotalCount { get; set; }

        public Dictionary<string, string> SqlWhereStatement { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        
        public Dictionary<string, string> SqlOrderStatements { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public Pagination Pagination { get; set; } = new Pagination();
        
        public Dictionary<string, EntityNode> EntityNodesApplied { get; set; }
        
        public Dictionary<string, Type> SplitOnDapper { get; set; } = new Dictionary<string, Type>(StringComparer.InvariantCultureIgnoreCase);

        public Dictionary<string, EntityNodeTree> EntityTrees { get; set; } = new Dictionary<string, EntityNodeTree>(StringComparer.InvariantCultureIgnoreCase);
        
        public Dictionary<string, ModelNodeTree> ModelTrees { get; set; } = new Dictionary<string, ModelNodeTree>(StringComparer.InvariantCultureIgnoreCase);
        
        public ModelNodeTree RelativeTree { get; set; }
    }
}