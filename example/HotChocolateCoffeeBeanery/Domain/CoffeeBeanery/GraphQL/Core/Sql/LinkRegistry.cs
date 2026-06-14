using CoffeeBeanery.GraphQL.Core.GraphQL;

namespace CoffeeBeanery.GraphQL.Core.Sql;

public static class LinkRegistry
{
    public static List<LinkKey> Links { get; } = new();

    public static LinkKey? Find(NodeTree parent, NodeTree child)
    {
        return Links.FirstOrDefault(l =>
            l.To == parent.Name &&
            l.From == child.Name);
    }
}