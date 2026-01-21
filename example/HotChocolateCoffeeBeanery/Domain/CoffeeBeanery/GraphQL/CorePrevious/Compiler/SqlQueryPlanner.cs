using CoffeeBeanery.GraphQL.Core.Compiler;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

internal static class SqlQueryPlanner
{
    public static void Plan(
        NodeTree node,
        SqlCompilationContext ctx,
        EntityMappingRegistry registry)
    {
        var map = registry.Get(node.Name);

        // SELECT columns
        foreach (var field in map.Fields)
        {
            var key = $"{node.Name}.{field.Value}";
            if (!ctx.SelectNodes.ContainsKey(key))
            {
                ctx.SelectNodes.Add(key, new SelectNode
                {
                    Entity = node.Name,
                    Column = field.Value,
                    Alias = $"{node.Name}_{field.Value}"
                });
            }
        }

        // JOIN children
        foreach (var child in node.Children)
        {
            var childMap = registry.Get(child.Name);

            var edgeKey = $"{node.Name}->{child.Name}";
            if (!ctx.EdgeNodes.ContainsKey(edgeKey))
            {
                ctx.EdgeNodes.Add(edgeKey, new EdgeNode
                {
                    FromEntity = node.Name,
                    FromColumn = childMap.ParentKey,
                    ToEntity = child.Name,
                    ToColumn = childMap.ChildKey,
                    IsInner = childMap.IsEdge
                });
            }

            Plan(child, ctx, registry);
        }
    }
}