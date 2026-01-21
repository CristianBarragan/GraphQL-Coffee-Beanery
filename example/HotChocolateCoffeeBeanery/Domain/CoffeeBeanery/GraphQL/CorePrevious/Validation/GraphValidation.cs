using System.Linq;
using CoffeeBeanery.GraphQL.Core.Sql;

public static class GraphValidation
{
    public static bool HasCycles(Dictionary<string, SqlNode> dict) =>
        dict.Values.Any(n => n.LinkKeys.Any(l => l.From == l.To));
}