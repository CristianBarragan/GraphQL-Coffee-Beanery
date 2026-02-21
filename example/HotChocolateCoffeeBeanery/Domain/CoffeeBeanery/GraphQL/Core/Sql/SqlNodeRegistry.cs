using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Helper;

namespace CoffeeBeanery.GraphQL.Core.Sql
{
    public static class SqlNodeRegistry
    {
        public static Dictionary<string, SqlNode> ModelNodes { get; } = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);
        
        public static Dictionary<string, SqlNode> EntityNodes { get; } = new Dictionary<string, SqlNode>(StringComparer.OrdinalIgnoreCase);
        
        public static Dictionary<string, NodeTree> ModelTrees { get; } = new Dictionary<string, NodeTree>(StringComparer.OrdinalIgnoreCase);
        
        public static Dictionary<string, NodeTree> EntityTrees { get; } = new Dictionary<string, NodeTree>(StringComparer.OrdinalIgnoreCase);
        
        public static List<string> EntityNames { get; } = new List<string>();
        
        public static List<string> ModelNames { get; } = new List<string>();

        public static void RegisterNode(string modelKey, string entityKey, SqlNode node)
        {
            ModelNodes[modelKey] = node;
            EntityNames.Add(entityKey);
            ModelNames.Add(modelKey);
            node.SqlNodeType = SqlNodeType.Node;

            if (EntityTrees.Any(a => a.Key.Matches(entityKey.Split('~')[0])))
            {
                EntityNodes[entityKey] = node;    
            }
        }
    }
}
