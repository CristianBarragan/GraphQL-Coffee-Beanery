using System.Collections.Generic;
using System.Linq;
using CoffeeBeanery.GraphQL.Core.Helper;
using CoffeeBeanery.GraphQL.Core.Mapping;
using HotChocolate.Execution.Processing;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.GraphQL.Core.Runtime;

namespace CoffeeBeanery.GraphQL.Core.Runtime
{
    internal static class SqlWhereCompiler
    {
        public static void Compile<D, S>(
            SqlCompilationContext ctx,
            ISelection rootSelection,
            IEntityTreeMap<D, S> entityTreeMap,
            IModelTreeMap<D, S> modelTreeMap,
            string rootEntityName,
            string wrapperEntityName)
            where D : class
            where S : class
        {
            // Prepare structures to hold where clauses
            var sqlWhereStatement = new Dictionary<string, string>();
            var whereFields = new List<string>();

            // Find the “where” AST argument
            var whereArg = rootSelection.SyntaxNode.Arguments
                .FirstOrDefault(a => a.Name.Value == "where");

            // If no where node provided, nothing to compile
            if (whereArg == null)
                return;

            // Invoke the AST-walker helper to collect where expressions
            SqlNodeResolverHelper.GetFieldsWhere(
                modelTreeMap.DictionaryTree,
                entityTreeMap.LinkDictionaryTreeNode,
                modelTreeMap.LinkDictionaryTreeNode,
                whereFields,
                sqlWhereStatement,
                whereArg.Value,
                modelTreeMap.DictionaryTree.Last().Value.Name,
                rootEntityName,
                wrapperEntityName,
                clauseCondition: "",
                clauseType: Entity.ClauseTypes,
                permission: null);

            // Merge into the compilation context
            foreach (var kv in sqlWhereStatement)
            {
                if (!ctx.Where.ContainsKey(kv.Key))
                    ctx.Where.Add(kv.Key, kv.Value);
                else
                    ctx.Where[kv.Key] += " AND " + kv.Value;
            }
        }
    }
    
    public class Entity
    {
        public static List<string> ClauseTypes = new List<string>()
        {
            "eq",
            "neq",
            "in",
            "any"
        };
    }
}
