using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;

public static class JoinResolver
{
    public static LinkKey? Resolve(
        NodeTree parent,
        NodeTree child,
        IEnumerable<LinkKey> links)
    {
        return links.FirstOrDefault(l =>
            l.To == parent.Name &&
            l.From == child.Name);
    }
}