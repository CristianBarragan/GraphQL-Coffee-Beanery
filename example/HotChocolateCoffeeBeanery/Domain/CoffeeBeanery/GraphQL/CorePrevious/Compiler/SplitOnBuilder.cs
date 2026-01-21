using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SplitOnBuilder
    {
        public static Dictionary<string, Type> Build(
            SqlCompilationContext ctx,
            Dictionary<string, Type> entityTypes)
        {
            var splitOn = new Dictionary<string, Type>();

            foreach (var edge in ctx.EdgeNodes.Values)
            {
                if (!splitOn.ContainsKey(edge.ToColumn))
                {
                    splitOn.Add(edge.ToColumn, entityTypes[edge.ToEntity]);
                }
            }

            return splitOn;
        }
    }
}