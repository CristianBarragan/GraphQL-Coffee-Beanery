// ProcessService.cs — wire everything with the corrected signatures
using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Engine;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using HotChocolateCoffeeBeanery.GraphQL.Core.Runtime;
using ExecutionContext = CoffeeBeanery.GraphQL.Core.Contracts.ExecutionContext;

public interface IProcessService<M> where M : class
{
    Task<QueryResult<M>> QueryProcessAsync(string cacheKey,
        ISelection selection,
        CancellationToken cancellationToken);

    Task<QueryResult<M>> MutationProcessAsync(string cacheKey,
        ISelection selection,
        CancellationToken cancellationToken);
}

public class ProcessService<M> : IProcessService<M>
    where M : class, new()
{
    private readonly IQueryEngine     _queryEngine;
    private readonly IMutationEngine  _mutationEngine;
    private readonly IHydrationEngine _hydrationEngine;
    private readonly IQueryPlanner    _queryPlanner;
    private readonly IGraphILCache    _graphCache;

    public ProcessService(
        IQueryEngine     queryEngine,
        IMutationEngine  mutationEngine,
        IHydrationEngine hydrationEngine,
        IQueryPlanner    queryPlanner,
        IGraphILCache    graphCache)
    {
        _queryEngine     = queryEngine;
        _mutationEngine  = mutationEngine;
        _hydrationEngine = hydrationEngine;
        _queryPlanner    = queryPlanner;
        _graphCache      = graphCache;
    }

    // =========================
    // QUERY
    // =========================
    public async Task<QueryResult<M>> QueryProcessAsync(
        string            cacheKey,
        ISelection        selection,
        CancellationToken cancellationToken)
    {
        var graph     = _graphCache.GetOrBuild(cacheKey, SqlNodeBuilder.Build);
        var rootAlias = graph.Nodes.Keys.First();

        var compilationCtx = new SqlCompilationContext();
        var compileResult  = SqlQueryCompiler.Compile(
            compilationCtx,
            (FieldNode)selection.SyntaxNode,
            graph,
            rootAlias,
            _queryPlanner);

        var request = new QueryRequest
        {
            Graph            = graph,
            Sql              = compileResult.Sql,
            QueryPlan        = _queryPlanner.Build(graph, rootAlias),
            ExecutionContext  = new ExecutionContext("", 30_000),
            HydrationContext = _hydrationEngine.BuildContext(
                                   graph, compileResult.SplitOnDapper)
        };

        var items = await _queryEngine.ExecuteAsync<M>(request, cancellationToken);
        return new QueryResult<M> { Items = items };
    }

    // =========================
    // MUTATION
    // =========================
    public async Task<QueryResult<M>> MutationProcessAsync(
        string            cacheKey,
        ISelection        selection,
        CancellationToken cancellationToken)
    {
        var graph     = _graphCache.GetOrBuild(cacheKey, SqlNodeBuilder.Build);
        var rootAlias = graph.Nodes.Keys.First();

        // 1. Extract the wrapper argument (contains the input object tree)
        var wrapperArg = selection.SyntaxNode.Arguments?
            .FirstOrDefault(a =>
                a.Name.Value.Equals("wrapper", StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidOperationException(
                "Mutation is missing the 'wrapper' argument.");

        // 2. Compile mutation SQL — GraphIL-native pipeline
        var mutationSql = SqlMutationCompiler.Compile(graph, wrapperArg.Value);

        // ProcessService.MutationProcessAsync — add before SqlMutationCompiler.Compile
        Console.WriteLine("=== Graph nodes in ProcessService ===");
        foreach (var key in graph.Nodes.Keys)
            Console.WriteLine($"  {key}");

        Console.WriteLine("=== wrapperArg raw value ===");
        Console.WriteLine(wrapperArg.Value.ToString());
        
        
        // 3. Compile select SQL — same as query path
        var compilationCtx = new SqlCompilationContext();
        var compileResult  = SqlQueryCompiler.Compile(
            compilationCtx,
            (FieldNode)selection.SyntaxNode,
            graph,
            rootAlias,
            _queryPlanner);

        var hydrationContext = _hydrationEngine.BuildContext(
            graph, compileResult.SplitOnDapper);

        // 4. Single round trip through MutationEngine
        // ProcessService.cs — MutationProcessAsync, no change needed here
        var mutationRequest = new MutationRequest
        {
            Graph            = graph,
            InsertSql        = mutationSql.InsertSql,      // Phase 1: relational upserts only
            GraphMergeSql    = mutationSql.GraphMergeSql,  // Phase 2: ag_catalog.cypher MERGE
            SelectSql        = compileResult.Sql,           // Phase 3: read back
            QueryPlan        = _queryPlanner.Build(graph, rootAlias),
            HydrationContext = hydrationContext,
            ExecutionContext  = new ExecutionContext("", 30_000)
        };

        var items = await _mutationEngine.ExecuteAsync<M>(
            mutationRequest, cancellationToken);

        return new QueryResult<M> { Items = items };
    }
}