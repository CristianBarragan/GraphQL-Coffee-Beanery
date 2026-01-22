using System.Collections.Generic;
using System.Linq;
using KeyNotFoundException = System.Collections.Generic.KeyNotFoundException;

namespace CoffeeBeanery.GraphQL.Core.GraphQL
{
    public static class NodeTreeBuilder
    {
        public static NodeTree Build(string rootName, Dictionary<string, NodeTree> nodes)
        {
            if (!nodes.ContainsKey(rootName))
                throw new KeyNotFoundException("Root node not found.");

            var root = nodes[rootName];

            foreach (var node in nodes.Values)
            {
                if (!string.IsNullOrEmpty(node.ParentName))
                {
                    if (nodes.TryGetValue(node.ParentName, out var parent))
                    {
                        parent.Children.Add(node);
                    }
                }
            }

            return root;
        }
    }
}