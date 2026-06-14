using CoffeeBeanery.GraphQL.Core.Contracts;
using CoffeeBeanery.GraphQL.Core.GraphQL;

namespace CoffeeBeanery.GraphQL.Core.Runtime;

internal static class FkResolver
{
    internal sealed record FkLink(
        NodeTree ParentTree,
        string FkColumn,
        string PkColumn,
        string CteName,
        List<MutationField> ParentFields,
        List<string> OnConflictCols
    );

    public static List<FkLink> Resolve(NodeTree node, IReadOnlyDictionary<string, NodeTree> entityTrees)
    {
        var result = new List<FkLink>();

        var map = node.NodeMap;
        if (map == null)
            return result;

        foreach (var link in map.EntityChildren.Concat(map.EntityRelatedChildren))
        {
            if (!entityTrees.TryGetValue(link.AliasTo, out var parentTree))
                continue;

            result.Add(new FkLink(
                ParentTree: parentTree,
                FkColumn: link.FromColumn,
                PkColumn: "Id",
                CteName: $"cte_{link.AliasTo}",
                ParentFields: new List<MutationField>(), // fill from plan later
                OnConflictCols: map.UpsertKeys.Select(k => k.Key).ToList()
            ));
        }

        return result;
    }
}