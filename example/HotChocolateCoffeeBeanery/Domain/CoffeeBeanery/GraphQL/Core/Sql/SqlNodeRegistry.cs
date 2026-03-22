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
        
        public static List<Type> EntityTypes { get; } = new List<Type>();
        
        public static List<Type> ModelTypes { get; } = new List<Type>();

        public static void RegisterNode(string modelKey, string entityKey, SqlNode node)
        {
            ModelNodes[modelKey] = node;
            // var id = int.Parse(node.Id) - 1;
            //
            // if (!EntityNames.Contains(entityKey.Split('~')[0]))
            // {
            //     if (EntityNames.Count < id)
            //     {
            //         EntityNames.Add(entityKey.Split('~')[0]);
            //     }
            //     else
            //     {
            //         EntityNames.Insert(id ,entityKey.Split('~')[0]);
            //     }  
            // }
            //
            // if (!ModelNames.Contains(modelKey.Split('~')[0]))
            // {
            //     if (ModelNames.Count < id)
            //     {
            //         ModelNames.Add(modelKey.Split('~')[0]);
            //     }
            //     else
            //     {
            //         ModelNames.Insert(id , modelKey.Split('~')[0]);
            //     } 
            // }
            //
            if (EntityTrees.Any(a => a.Key.Matches(entityKey.Split('~')[0])))
            {
                EntityNodes[entityKey] = node;    
            }
        }
    }
}
