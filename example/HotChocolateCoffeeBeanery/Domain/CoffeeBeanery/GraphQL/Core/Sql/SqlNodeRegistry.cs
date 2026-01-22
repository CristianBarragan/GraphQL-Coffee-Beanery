using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.GraphQL;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public static class SqlNodeRegistry
    {
        public static Dictionary<string, SqlNode> NodeNodes { get; } = new();
        public static Dictionary<string, SqlNode> EdgeNodes { get; } = new();
        public static Dictionary<string, SqlNode> MutationNodes { get; } = new();
        
        public static Dictionary<string, NodeTree> ModelTrees { get; } = new();
        
        public static Dictionary<string, NodeTree> EntityTrees { get; } = new();

        public static void RegisterNode(string key, SqlNode node)
        {
            NodeNodes[key] = node;
        }

        public static void RegisterEdge(string key, SqlNode node)
        {
            EdgeNodes[key] = node;
        }

        public static void RegisterMutation(string key, SqlNode node)
        {
            MutationNodes[key] = node;
        }
    }
}
