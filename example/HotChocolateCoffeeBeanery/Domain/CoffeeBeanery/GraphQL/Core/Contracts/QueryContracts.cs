using System.Reflection;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Engine;

namespace CoffeeBeanery.GraphQL.Core.Contracts;

public class QueryRequest
{
    public PlanningContext PlanningContext { get; set; }
    public NodeTree Root { get; set; }
    public string Sql { get; set; } = string.Empty;
    public object? Parameters { get; set; }
    public QueryPlan? QueryPlan { get; set; }
    
    public MutationPlan? MutationPlan { get; set; }

    public GraphIL Graph { get; set; }

    public OptimizationContext OptimizationContext { get; set; }
    public ExecutionContext ExecutionContext { get; set; }
    public HydrationContext HydrationContext { get; set; }
}

// ===============================
// Optimization Context (REAL VERSION)
// ===============================
public sealed record OptimizationContext
{
    public required string QueryText { get; init; }
    public required string ShapeKey { get; init; }
    public IReadOnlyDictionary<string, object>? Metadata { get; init; }

    public ExecutionHistory ExecutionHistory { get; set; } = new();
}

public sealed record MutationRenderResult(
    string sql
);

public sealed record MutationPlan(
    IReadOnlyDictionary<string, MutationNode> Nodes,
    IReadOnlyList<MutationStatement> Statements,
    MutationNode Root
);

public sealed class MutationNode
{
    public string Alias { get; }
    public string EntityName { get; }
    public string Operation { get; } // UPSERT / INSERT / UPDATE

    public List<MutationField> IdentityFields { get; }
    public List<MutationField> DataFields { get; }

    public Type EntityType { get; }

    public MutationNode(
        string alias,
        string entityName,
        string operation,
        List<MutationField> identityFields,
        List<MutationField> dataFields,
        Type entityType)
    {
        Alias = alias;
        EntityName = entityName;
        Operation = operation;
        IdentityFields = identityFields;
        DataFields = dataFields;
        EntityType = entityType;
    }
}

public sealed record MutationStatement(
    string Sql,
    string TargetAlias
);

public sealed record MutationField(
    string Name,
    string Column,
    object? Value
);

// ===============================
// Planning Context
// ===============================

public sealed class PlanningContext
{
    public NodeTree Root { get; set; } = default!;

    public Dictionary<string, Dictionary<string, object?>> MutationValues { get; set; }
        = new();
}

// ===============================
// Hydration Context
// ===============================
public sealed record MemberMapping(
    int Ordinal,
    MemberInfo Member,
    Type MemberType);

public sealed record SqlCompileResult(
    string Sql,
    ProjectionMap ProjectionMap,
    Dictionary<string, Type> SplitOnDapper);

public sealed class TypeMetadata
{
    public required Dictionary<string, MemberInfo> Members { get; init; }
}

public static class Schema
{
    public static IReadOnlyDictionary<string, NodeTree> ModelTrees;
}

public sealed record OrderInstruction(
    string? Alias,
    string Field,
    SortDirection Direction);

public sealed record QueryFilter(
    string Field,
    string Operator,
    object? Value
);

public enum SortDirection
{
    Asc,
    Desc
}

public sealed class NodeMetadata
{
    public bool IsEntity { get; set; }
    public string? Prefix { get; set; }
    public string? ModelName { get; set; }
}

public class QueryResult<M> where M :class
{
    public IList<M> Items {
        get;
        set;
    }
}

public sealed class MutationResult
{
    public bool Success { get; set; }

    public List<MutationError> Errors { get; set; } = new();

    // Optional but very useful for debugging / GraphQL responses
    public int AffectedRows { get; set; }

    public static MutationResult Ok(int affectedRows = 0)
        => new MutationResult
        {
            Success = true,
            AffectedRows = affectedRows
        };

    public static MutationResult Fail(params MutationError[] errors)
        => new MutationResult
        {
            Success = false,
            Errors = errors.ToList(),
            AffectedRows = 0
        };
}

public class MutationError
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;

    public string? Path { get; set; }
    public string? Alias { get; set; }

    public static MutationError Create(string code, string message, string? alias = null, string? path = null)
    {
        return new MutationError
        {
            Code = code,
            Message = message,
            Alias = alias,
            Path = path
        };
    }
}

// ===============================
// CORE QUERY INTERFACE (FIXED)
// ===============================

public interface IQuery<TInput, TResult>
{
    Task<TResult> ExecuteAsync(TInput input, CancellationToken ct);
}

// ===============================
// OPTIMIZER CONTRACT (FIXED)
// ===============================
public interface IQueryOptimizer
{
    QueryPlan Optimize(QueryPlan plan, OptimizationContext context);
}

public sealed record SqlRenderResult(
    string Sql,
    ProjectionEntry ProjectionEntry
);

public interface IRowHydrator<T>
{
    T Hydrate(object[] row);
}

public interface IQueryTraceCollector
{
    void PlanBuilt(QueryPlan plan);

    void Optimized(QueryPlan original, QueryPlan optimized);

    void SqlGenerated(string sql);

    void ExecutionStarted(string sql);

    void ExecutionCompleted(int rowCount, long elapsedMs);

    void Error(Exception exception);
}

public sealed record ExecutionContext(
    string ConnectionString,
    int TimeoutMs,
    IReadOnlyDictionary<string, object>? Parameters = null
);

public class QueryPlan
{
    public IReadOnlyDictionary<string, PlanNode> Nodes { get; set; }
    public IReadOnlyList<PlanJoin>               Joins { get; set; }
    public PlanNode                              RootAlias   { get; set; }

    // Added — populated by SqlQueryCompiler before passing to SqlRenderer
    public List<OrderInstruction>                OrderBy     { get; set; } = new();
    public Dictionary<string, string>            Where       { get; set; } = new();
    public Pagination                            Pagination  { get; set; } = new();
    public bool                                  HasPagination { get; set; }
}

public sealed record SqlProjectionColumn(
    string EntityAlias,
    string FieldName,
    int Ordinal
);

public sealed record PlanNode(
    string Alias,
    string Table,
    string Schema,
    bool Required,
    IReadOnlyList<string> Columns,
    Type EntityType
);

public sealed record PlanJoin(
    string FromAlias,
    string ToAlias,
    string FromColumn,
    string ToColumn
);

// public sealed class QueryPlan
// {
//     public Dictionary<string, PlanNode> Nodes { get; } = new();
//     public List<PlanJoin> Joins { get; } = new();
//
//     
// }

// public sealed class PlanNode
// {
//     public string Alias { get; set; } = "";
//     public string Table { get; set; } = "";
//     public bool Required { get; set; }
//
//     public List<string> Columns { get; set; } = new();
// }

// public sealed class PlanJoin
// {
//     public string FromAlias { get; set; } = "";
//     public string ToAlias { get; set; } = "";
//
//     public string FromColumn { get; set; } = "";
//     public string ToColumn { get; set; } = "";
// }
//
// public sealed class NodeRelation
// {
//     public string FromAlias { get; set; } = "";
//     public string ToAlias { get; set; } = "";
//
//     public string FromColumn { get; set; } = "";
//     public string ToColumn { get; set; } = "";
// }

public sealed class GraphNodePlan
{
    // identity
    public string Alias { get; init; } = "";
    public string Table { get; init; } = "";

    // projection
    public List<SelectColumn> Columns { get; init; } = new();

    // relationships (runtime wiring)
    public List<GraphEdgePlan> Children { get; init; } = new();

    // optional flags
    public bool IsRoot { get; init; }
    public bool IsRequired { get; init; }
}

public sealed class GraphEdgePlan
{
    public string FromAlias { get; init; } = "";
    public string ToAlias { get; init; } = "";

    public string FromColumn { get; init; } = "";
    public string ToColumn { get; init; } = "";
}

public sealed class ProjectionMap
{
    public List<SelectColumn> Columns { get; } = new();

    public ProjectionMap() { }

    public ProjectionMap(IEnumerable<SelectColumn> columns)
    {
        Columns = columns.ToList();
    }
}

public sealed class ProjectionEntry
{
    public string Alias { get; init; } = default!;
    public string Column { get; init; } = default!;
    public int Index { get; init; }
}


public class SelectColumn
{
    public string Alias { get; set; } = default!;
    public string Column { get; set; } = default!;
    public string Property { get; set; } = default!;
    public int Index { get; set; }
    public Type TargetType { get; set; } = typeof(object);
}