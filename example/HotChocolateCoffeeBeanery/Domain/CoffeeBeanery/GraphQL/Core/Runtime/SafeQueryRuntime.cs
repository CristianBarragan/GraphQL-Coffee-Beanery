using CoffeeBeanery.GraphQL.Core.Contracts;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public class SafeQueryRuntime<TInput, TResult>
    where TInput : class
{
    private readonly IQuery<TInput, TResult> _inner;
    private readonly ILogger<SafeQueryRuntime<TInput, TResult>> _logger;

    public SafeQueryRuntime(
        IQuery<TInput, TResult> inner,
        ILogger<SafeQueryRuntime<TInput, TResult>> logger)
    {
        _inner = inner;
        _logger = logger;
    }

    public async Task<TResult> ExecuteAsync(
        TInput request,
        CancellationToken ct)
    {
        try
        {
            return await _inner.ExecuteAsync(request, ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "GraphQL query failed safely");

            // return safe default
            return default!;
        }
    }
}