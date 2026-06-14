// GraphEngine.cs
using System.Text;

public interface IGraphEngine
{
    /// <summary>
    /// Runs relational INSERT statements sequentially as plain SQL.
    /// Cannot be CTEs — later inserts SELECT from rows written by earlier ones.
    /// </summary>
    string BuildInsertOnlySql(string insertSql);
}

public sealed class GraphEngine : IGraphEngine
{
    public string BuildInsertOnlySql(string insertSql)
    {
        if (string.IsNullOrWhiteSpace(insertSql))
            return string.Empty;

        // Compiler emits ";;" as the statement separator.
        // Single ";" appears inside INSERT … SELECT sub-queries — can't split on that.
        var statements = insertSql
            .Split(new[] { ";;" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .ToList();

        if (statements.Count == 0)
            return string.Empty;

        // Plain sequential SQL — each INSERT sees committed rows from the ones above it.
        // Statements 4 & 5 SELECT from Customer rows written by statements 1 & 2.
        var sb = new StringBuilder();
        foreach (var stmt in statements)
        {
            sb.Append(stmt);
            sb.AppendLine(";");
        }

        return sb.ToString();
    }
}