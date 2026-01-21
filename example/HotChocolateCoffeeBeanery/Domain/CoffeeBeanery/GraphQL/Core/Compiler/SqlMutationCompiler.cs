using System;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Execution.Processing;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public static class SqlMutationCompiler
    {
        public static string CompileMutation<TModel>(
            ISelection rootSelection,
            NodeTree rootNode)
            where TModel : class
        {
            var ctx = new SqlCompilationContext();
            var (select, edge, mutation) = SqlNodeResolver.ResolveFromSelection<TModel>(rootSelection, rootNode.Name, true);

            // mutation logic omitted for brevity (same style)
            return "NOT IMPLEMENTED";
        }
    }
}