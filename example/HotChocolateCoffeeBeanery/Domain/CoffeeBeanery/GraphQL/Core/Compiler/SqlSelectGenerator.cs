using System.Text;
using CoffeeBeanery.GraphQL.Core.Extension;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.GraphQL.Core.Compiler
{
    public sealed class SqlSelectResult
    {
        public string Query { get; set; } = "";
        public IReadOnlyDictionary<string, Type> SplitOnDapper { get; set; } = new Dictionary<string, Type>();
    }

    public static class SqlSelectGenerator
    {
        public static SqlSelectResult BuildSelect(
            SqlCompilationContext ctx,
            Dictionary<string, SqlNode> nodes,
            NodeTree root)
        {
            var sb = new StringBuilder();
            sb.Append("SELECT ");

            // recursively build select columns
            var columns = new List<string>();
            CollectColumns(root, nodes, columns);

            sb.Append(string.Join(",", columns));
            sb.Append($" FROM \"{root.Schema}\".\"{root.Name}\" {root.Name}");

            BuildJoins(sb, root, nodes);

            if (ctx.TryGetWhere(root.Name, out var w))
                sb.Append(" WHERE " + w);

            if (ctx.HasSorting)
                sb.Append(" ORDER BY " + string.Join(",", ctx.OrderClauses));

            if (ctx.HasPagination)
                sb.Append(" LIMIT " + ctx.Pagination.First);

            // -------------------------
            // Build SplitOnDapper here
            // -------------------------
            var splitOn = BuildSplitOnDapper(root, nodes);

            return new SqlSelectResult
            {
                Query = sb.ToString(),
                SplitOnDapper = splitOn
            };
        }

        private static void CollectColumns(
            NodeTree tree,
            Dictionary<string, SqlNode> nodes,
            List<string> columns)
        {
            // collect node columns
            foreach (var n in nodes.Values.Where(x => x.EntityName == tree.Name && x.SqlNodeType == SqlNodeType.Select))
            {
                columns.Add($"{tree.Name}.\"{n.ColumnName}\"");
            }

            // recurse children
            foreach (var child in tree.Children)
            {
                CollectColumns(child, nodes, columns);
            }
        }

        private static void BuildJoins(
            StringBuilder sb,
            NodeTree tree,
            Dictionary<string, SqlNode> nodes)
        {
            foreach (var child in tree.Children)
            {
                var joinKey = $"{tree.Name}~{child.Name}";
                if (nodes.TryGetValue(joinKey, out var n))
                {
                    // use inner join if edge, else left join
                    var joinType = n.SqlNodeType == SqlNodeType.Edge ? "JOIN" : "LEFT JOIN";

                    sb.Append($" {joinType} \"{child.Schema}\".\"{child.Name}\" {child.Name} ON ");
                    sb.Append($"{tree.Name}.\"Id\" = {child.Name}.\"{child.ParentName}Id\""); // assumes standard FK
                }

                BuildJoins(sb, child, nodes);
            }
        }

        private static IReadOnlyDictionary<string, Type> BuildSplitOnDapper(
            NodeTree root,
            Dictionary<string, SqlNode> nodes)
        {
            var dict = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);

            // root entity
            var rootType = GetEntityTypeFromNode(nodes, root.Name);
            if (rootType != null)
                dict["Id"] = rootType;

            // traverse tree to collect children
            CollectSplitOn(root, nodes, dict);

            return dict;
        }

        private static void CollectSplitOn(
            NodeTree tree,
            Dictionary<string, SqlNode> nodes,
            Dictionary<string, Type> dict)
        {
            foreach (var child in tree.Children)
            {
                var childType = GetEntityTypeFromNode(nodes, child.Name);
                if (childType != null && !dict.ContainsKey("Id".ToSnakeCase(child.Id)))
                {
                    dict["Id".ToSnakeCase(child.Id)] = childType;
                }

                CollectSplitOn(child, nodes, dict);
            }
        }

        private static Type GetEntityTypeFromNode(Dictionary<string, SqlNode> nodes, string entityName)
        {
            // nodes store Type as EntityType
            var node = nodes.Values.FirstOrDefault(n => n.EntityName.Equals(entityName, StringComparison.OrdinalIgnoreCase));
            return node?.EntityType;
        }
    }
}
