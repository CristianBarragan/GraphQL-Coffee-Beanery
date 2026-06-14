using Npgsql;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public class CompiledPlan
{
    public Func<NpgsqlConnection, Task<List<object>>> Execute { get; set; }
}