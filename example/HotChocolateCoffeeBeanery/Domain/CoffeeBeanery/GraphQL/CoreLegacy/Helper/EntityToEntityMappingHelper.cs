
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Helper;

public class EntityMapping<TModel, TEntity>
    where TModel : class
    where TEntity : class
{
    public Type ModelType => typeof(TModel);
    public Type EntityType => typeof(TEntity);

    public List<FieldMap> FieldMaps { get; } = new();
    public List<LinkMap> Links { get; } = new();
    public List<UpsertKey> UpsertKeys { get; } = new();
}
