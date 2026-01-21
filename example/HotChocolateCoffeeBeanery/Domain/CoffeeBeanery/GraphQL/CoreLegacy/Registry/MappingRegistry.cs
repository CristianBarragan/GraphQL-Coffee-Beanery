using CoffeeBeanery.GraphQL.Core.Helper;

public static class MappingRegistry
{
    private static readonly Dictionary<Type, object> _maps = new();

    public static void Register<TModel>(ModelMapping<TModel> map)
        where TModel : class
    {
        _maps[typeof(TModel)] = map;
    }

    public static ModelMapping<TModel> Get<TModel>()
        where TModel : class
    {
        if (_maps.TryGetValue(typeof(TModel), out var m))
            return (ModelMapping<TModel>)m;

        throw new KeyNotFoundException($"Mapping for {typeof(TModel).Name} not found.");
    }
}