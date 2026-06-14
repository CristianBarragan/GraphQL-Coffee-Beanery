using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

public static class SqlQueryStructureBuilder
{
    public static List<SqlQueryStructure> Build(GraphIL graph)
    {
        var result = new List<SqlQueryStructure>();

        foreach (var node in graph.Nodes.Values)
        {
            result.Add(new SqlQueryStructure
            {
                Name = node.TableName,
                Alias = node.Alias,
                Columns = node.Columns.ToList(),
                LinkKeys = node.UpsertKeys.Select(k => new LinkKey
                {
                    AliasFrom = node.Alias,
                    AliasTo = node.Alias,
                    FromColumn = k,
                    ToColumn = k
                }).ToList()
            });
        }

        return result;
    }
}