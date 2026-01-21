using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Compiler;

public static class SqlMutationGenerator
{
    public static string BuildUpsert(
        SqlCompilationContext ctx,
        string entity,
        string schema)
    {
        var cols = ctx.MutationNodes.Values.Select(m => $"\"{m.Column}\"");
        var vals = ctx.MutationNodes.Values.Select(m => m.Parameter);

        return $@"
            INSERT INTO ""{schema}"".""{entity}""
            ({string.Join(",", cols)})
            VALUES ({string.Join(",", vals)})
            ON CONFLICT (id) DO UPDATE SET
            {string.Join(",", ctx.MutationNodes.Values.Select(m => $"\"{m.Column}\" = {m.Parameter}"))};
            ";
    }
}
