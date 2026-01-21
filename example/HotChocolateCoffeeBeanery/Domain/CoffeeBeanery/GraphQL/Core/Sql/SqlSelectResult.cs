namespace CoffeeBeanery.GraphQL.Core.Sql;

public sealed class SqlSelectResult
{
    public string Query { get; set; }
    public IReadOnlyDictionary<string, Type> SplitOnDapper { get; set; }
}
