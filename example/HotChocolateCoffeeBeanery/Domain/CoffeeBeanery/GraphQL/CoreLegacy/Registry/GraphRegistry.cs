using System;
using System.Collections.Concurrent;
using CoffeeBeanery.GraphQL.Core.Keys;
using CoffeeBeanery.GraphQL.Core.Relationships;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Registry;

public static class GraphRegistry
{
    public static ConcurrentDictionary<NodeKey, object> Nodes { get; } = new();
    public static ConcurrentDictionary<RelationshipKey, object> Relationships { get; } = new();

    // NEW
    public static ConcurrentDictionary<string, SqlNode> SqlNodes { get; } = new();

    public static void AddNode(string entity, string field, object node)
    {
        Nodes[new NodeKey(entity, field)] = node;

        // If it is SqlNode, add to SqlNodes too
        if (node is SqlNode sqlNode)
            SqlNodes[$"{entity}.{field}"] = sqlNode;
    }

    public static void AddRelationship(string from, string to, object relationship)
    {
        Relationships[new RelationshipKey(from, to)] = relationship;
    }

    public static bool TryGetNode(string key, out object node)
        => Nodes.TryGetValue(NodeKey.Parse(key), out node!);

    public static bool TryGetRelationship(string key, out object rel)
        => Relationships.TryGetValue(RelationshipKey.Parse(key), out rel!);
}