using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SqlQueryCompiler
    {
        public static string Compile(
            ISelection selection,
            NodeTree root)
        {
            var ctx = new SqlCompilationContext();
            var nodes = SqlNodeResolver.ResolveFromSelection(selection, root, false);
            return SqlSelectGenerator.BuildSelect(ctx, nodes, root);
        }
    }
}