using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.Sql;
using HotChocolate.Execution.Processing;
using HotChocolate.Language;

public static class MutationCompiler
{
    public static string Compile<TModel>(
        ISelection fieldSelection,
        Dictionary<string, SqlNode> nodes,
        string rootEntityName)
    {
        // Walk AST, fill node values based on selections
        foreach (var kv in nodes)
        {
            if (fieldSelection.Fields.Contains(kv.Key.Split('~')[1]))
                kv.Value.MutationType = SqlNodeType.Update;
        }

        return SqlHelper.GenerateMerge(nodes, rootEntityName, null);
    }
}