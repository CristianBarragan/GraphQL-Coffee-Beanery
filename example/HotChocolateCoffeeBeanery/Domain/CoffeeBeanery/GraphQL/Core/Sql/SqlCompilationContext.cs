using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Runtime;

namespace CoffeeBeanery.GraphQL.Core.Sql;

public class SqlCompilationContext
{
    public NodeTree RootTree { get; set; } = default!;

    // SQL outputs
    public string SelectSql { get; set; } = string.Empty;
    public string UpsertSql { get; set; } = string.Empty;

    // Pagination
    public Pagination Pagination { get; set; } = new();

    // Execution cache
    public ExecutionCache ExecutionCache { get; set; } = new();

    // Dapper multi-mapping support
    public Dictionary<string, Type> SplitOnDapper { get; set; } = new();

    // Tracking
    public bool HasTotalCount { get; set; }
    public bool HasPagination { get; set; }

    public IDictionary<string, string> SqlWhereStatement { get; set; }

    public bool RequiresTotalCount { get; set; }
    
    public ProjectionMap Projection { get; set; } = new();

    public IDictionary<string, NodeTree> ModelTrees { get; set; }
    
    public IDictionary<string, NodeTree> EntityTrees { get; set; }
    
    public NodeTree RelativeTree { get; set; }
    
}