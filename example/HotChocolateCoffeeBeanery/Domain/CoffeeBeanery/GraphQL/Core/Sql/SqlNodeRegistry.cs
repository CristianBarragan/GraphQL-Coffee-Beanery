using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.GraphQL;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public static class SqlNodeRegistry
    {
        public static Dictionary<string, SqlNode> ModelNodeNodes { get; } = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, SqlNode> ModelEdgeNodes { get; } = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, SqlNode> ModelMutationNodes { get; } = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);
        
        public static Dictionary<string, SqlNode> EntityNodeNodes { get; } = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, SqlNode> EntityEdgeNodes { get; } = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);
        public static Dictionary<string, SqlNode> EntityMutationNodes { get; } = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);
        
        public static Dictionary<string, NodeTree> ModelTrees { get; } = new Dictionary<string, NodeTree>(StringComparer.OrdinalIgnoreCase);
        
        public static Dictionary<string, NodeTree> EntityTrees { get; } = new Dictionary<string, NodeTree>(StringComparer.OrdinalIgnoreCase);

        public static void RegisterNode(string modelKey, string entityKey, SqlNode node)
        {
            ModelNodeNodes[modelKey] = node;
            EntityNodeNodes[entityKey] = node;
        }

        public static void RegisterEdge(string modelKey, string entityKey, SqlNode node)
        {
            ModelEdgeNodes[modelKey] = node;
            EntityEdgeNodes[entityKey] = node;
        }

        public static void RegisterMutation(string modelKey, string entityKey, SqlNode node)
        {
            ModelMutationNodes[modelKey] = node;
            EntityMutationNodes[entityKey] = node;
        }
    }
}
