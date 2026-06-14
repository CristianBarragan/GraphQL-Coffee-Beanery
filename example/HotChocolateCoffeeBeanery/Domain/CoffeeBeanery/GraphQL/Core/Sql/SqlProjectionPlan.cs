using CoffeeBeanery.GraphQL.Core.Contracts;

namespace CoffeeBeanery.GraphQL.Core.Sql;
public sealed record ProjectionColumn(
    string EntityAlias,
    string FieldName,
    int Ordinal
);

public sealed class SqlProjectionPlan
{
    private readonly List<ProjectionColumn> _columns = new();

    public IReadOnlyList<ProjectionColumn> Columns => _columns;

    public void Add(
        string entityAlias,
        string fieldName)
    {
        _columns.Add(
            new ProjectionColumn(
                entityAlias,
                fieldName,
                _columns.Count));
    }

    public bool HasFields(string entityAlias)
    {
        return _columns.Any(x =>
            x.EntityAlias.Equals(
                entityAlias,
                StringComparison.OrdinalIgnoreCase));
    }

    public IEnumerable<string> GetEntities()
    {
        return _columns
            .Select(x => x.EntityAlias)
            .Distinct(StringComparer.OrdinalIgnoreCase);
    }

    public IEnumerable<ProjectionColumn> GetColumns(
        string entityAlias)
    {
        return _columns.Where(x =>
            x.EntityAlias.Equals(
                entityAlias,
                StringComparison.OrdinalIgnoreCase));
    }
}