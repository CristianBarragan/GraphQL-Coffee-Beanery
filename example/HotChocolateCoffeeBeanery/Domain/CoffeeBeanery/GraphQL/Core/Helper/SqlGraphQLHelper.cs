using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.GraphQL;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Helper
{
    public static class SqlGraphQLHelper
    {
        public static string TranslateFilter(
            NodeTree nodeTree,
            Dictionary<string, SqlNode> nodeDict,
            string field,
            string filterType,
            string value)
        {
            if (!nodeDict.TryGetValue($"{nodeTree.Name}~{field}", out var node))
                return "";

            return filterType switch
            {
                "=" => $"{nodeTree.Name}.\"{node.Column}\" = '{value}'",
                "<>" => $"{nodeTree.Name}.\"{node.Column}\" <> '{value}'",
                _ => ""
            };
        }
    }
}