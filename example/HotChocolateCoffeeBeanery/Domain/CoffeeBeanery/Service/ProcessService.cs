using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.Runtime;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.Service.Materialization;
using Dapper;
using FASTER.core;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;
using Npgsql;

namespace CoffeeBeanery.Service;

public interface IProcessService<M> where M : class
{
    Task<QueryResult<M>> QueryProcessAsync(string cacheKey, ISelection selection, string modelName, CancellationToken cancellationToken);
    Task<QueryResult<M>> MutationProcessAsync(string cacheKey, ISelection selection, string modelName, CancellationToken cancellationToken);
}

public class ProcessService<M> : IProcessService<M>
    where M : class
{
    private readonly IQueryDispatcher _queryDispatcher;
    private readonly NpgsqlDataSource _dataSource;
    private readonly IFasterKV<string, string> _cache;

    public ProcessService(
        IQueryDispatcher queryDispatcher,
        NpgsqlDataSource dataSource,
        IFasterKV<string, string> cache)
    {
        _queryDispatcher = queryDispatcher;
        _dataSource = dataSource;
        _cache = cache;
    }

    public async Task<QueryResult<M>> QueryProcessAsync(
        string cacheKey,
        ISelection selection,
        string modelName,
        CancellationToken cancellationToken)
    {
        var rootTree = NodeRegistry.ModelTrees.First(x => x.Value.ModelName == modelName).Value;

        var context = new SqlCompilationContext();

        var entityTree = NodeRegistry.EntityTrees.First(x => x.Value.Alias == rootTree.Alias).Value;

        // SqlQueryCompiler.Compile(context, selection, entityTree, _cache, cacheKey);
        //
        // context.ModelTrees = NodeRegistry.ModelTrees;
        // context.EntityTrees = NodeRegistry.EntityTrees;
        // context.RelativeTree = rootTree;

        var parameters = new ProcessQueryParameters
        {
            Context = context,
            Model = modelName
        };

        var (models, startCursor, endCursor, totalCount, totalPageRecords) =
            await _queryDispatcher.DispatchAsync<ProcessQueryParameters, (List<M>, int?, int?, int?, int?)>(
                parameters, cancellationToken);

        return new QueryResult<M>
        {
            Models = models,
            StartCursor = startCursor,
            EndCursor = endCursor,
            TotalCount = totalCount ?? 0,
            TotalPageRecords = totalPageRecords ?? 0
        };
    }

    public async Task<QueryResult<M>> MutationProcessAsync(
    string cacheKey,
    ISelection selection,
    string modelName,
    CancellationToken cancellationToken)
{
    var rootTree = NodeRegistry.ModelTrees.First(x => x.Value.ModelName == modelName).Value;
    var entityTree = NodeRegistry.EntityTrees.First(x => x.Value.ModelName == rootTree.ModelName).Value;

    var mutationArgument = selection.SyntaxNode.Arguments
        .FirstOrDefault(a => a.Name.Value != "where" && a.Name.Value != "order");

    ExecutionPlan? mutationPlan = null;

    if (mutationArgument?.Value is ObjectValueNode obj)
    {
        var rootField = obj.Fields.FirstOrDefault(f => f.Value is ObjectValueNode or ListValueNode);
        if (rootField != null)
            mutationPlan = GraphMutationPlanBuilder.Build(entityTree.Alias, rootField.Value);
    }

    mutationPlan ??= GraphMutationPlanBuilder.Build(entityTree.Alias, new NullValueNode(null));

    var statements = new List<string>();
    var mergeStatements = new List<string>();

    SqlHelper.GenerateUpsertStatements(
        NodeRegistry.EntityTrees,
        mutationPlan,
        entityTree,
        new Dictionary<string, string>(),
        statements,
        mergeStatements);

    var selectionSet = selection.SyntaxNode.SelectionSet;

    var queryPlan = GraphQueryPlanBuilder.Build(rootTree.Alias, selectionSet);

    var select = SqlSelectStatementBuilder.Build(
        NodeRegistry.EntityTrees,
        queryPlan,
        entityTree,
        new Dictionary<string, string>(),
        new Dictionary<string, string>());

    var batchSql = string.Join("\n", statements.Concat(mergeStatements)) + "\n" + select.Sql;

    using var connection = await AgeConnectionFactory.OpenAsync(_dataSource);

    using var grid = await connection.QueryMultipleAsync(
        new CommandDefinition(batchSql, cancellationToken: cancellationToken));

    var rawRows = grid.Read(select.Types.ToArray(), objs => objs, splitOn: select.SplitOn);

    var rows = rawRows.Select(x => (object?[])x).ToList();

    var models = DynamicGraphMaterializer.Materialize<M>(
        queryPlan,
        select.NodeIdOrder,
        rows);

    return new QueryResult<M>
    {
        Models = models,
        TotalCount = models.Count,
        TotalPageRecords = models.Count
    };
}
}