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

        public static void RegisterNode(string modelKey, string entityKey, SqlNode node, Type modelType, Type entityType, bool isEntity)
        {
            var modelKeyAux = modelKey.Split('~').Length > 1 ? ModelTrees.FirstOrDefault(a => a.Key.Matches(modelKey.Split('~')[0])).Value?.Alias ?? modelKey.Split('~')[0] : modelKey.Split('~')[1];
            var entityKeyAux = entityKey.Split('~').Length > 1 ? EntityTrees.FirstOrDefault(a => a.Key.Matches(entityKey.Split('~')[0])).Value?.Alias ?? entityKey.Split('~')[0] : entityKey.Split('~')[1];
            
            if (!ModelNames.Contains(modelKeyAux))
            {
                ModelNames.Add(modelKeyAux);
            }
            
            if (!EntityNames.Contains(entityKeyAux) && isEntity)
            {
                EntityNames.Add(entityKeyAux);
            }
            
            if (!ModelTypes.Contains(modelType))
            {
                ModelTypes.Add(modelType);    
            }
            
            if (!EntityTypes.Contains(entityType) && isEntity)
            {
                EntityTypes.Add(entityType);    
            }
            
            if (!EntityNodes.ContainsKey(entityKey))
            {
                EntityNodes[entityKey] = node;
            }
            
            if (!ModelNodes.ContainsKey(modelKey))
            {
                ModelNodes[modelKey] = node;
            }
        }
    }
}
