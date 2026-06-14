namespace CoffeeBeanery.GraphQL.Core.Sql;

public class SqlFragmentCache
{
    public Dictionary<string, string> FieldFragments { get; } = new();
    public Dictionary<string, List<string>> EntitySelects { get; } = new();
    public Dictionary<string, string> JoinFragments { get; } = new();
}

public class SubqueryCache
{
    // key = entity + required fields signature
    public Dictionary<string, string> EntitySubqueries { get; } = new();
}
