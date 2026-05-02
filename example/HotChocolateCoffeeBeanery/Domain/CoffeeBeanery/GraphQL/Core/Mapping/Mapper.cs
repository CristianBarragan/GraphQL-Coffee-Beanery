using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Mapping;

public interface IMapper
{
    TModel MapToModel<TModel>(object entity);
    TModel MapToModel<TModel>(object entity, string alias);
    object MapToEntity<TModel>(TModel model);
    object MapByAlias(Type entityType, object entity, string alias);
    TModel MapToUpdatedModel<TModel, TEntity, TKey>(
        TModel current,
        TEntity from,
        Func<TModel, TKey> modelKeySelector,
        params object[] nestedEntities)
        where TModel : class
        where TEntity : class;
}

public class Mapper : IMapper
{
    private readonly Dictionary<string, NodeMap> _mappings;
    private readonly ConcurrentDictionary<string, PropertyInfo[]> _propCache = new();
    private readonly ConcurrentDictionary<string, Func<object, object>> _getterCache = new();
    private readonly ConcurrentDictionary<string, Action<object, object>> _setterCache = new();

    public Mapper(Dictionary<string, NodeMap> mappings)
    {
        _mappings = mappings;
    }

    private PropertyInfo[] GetCachedProperties(Type type) =>
        _propCache.GetOrAdd(type.FullName!,
            _ => type.GetProperties(BindingFlags.Public | BindingFlags.Instance));

    private Func<object, object> GetCachedGetter(Type type, string propertyName)
    {
        var key = $"{type.FullName}.{propertyName}";
        return _getterCache.GetOrAdd(key, _ =>
        {
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop?.GetGetMethod() == null) return _ => null!;
            var target = Expression.Parameter(typeof(object), "target");
            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Call(Expression.Convert(target, type), prop.GetGetMethod()!),
                    typeof(object)),
                target).Compile();
        });
    }

    private Action<object, object> GetCachedSetter(Type type, string propertyName)
    {
        var key = $"{type.FullName}.{propertyName}";
        return _setterCache.GetOrAdd(key, _ =>
        {
            var prop = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
            if (prop?.GetSetMethod() == null) return (_, __) => { };
            var target = Expression.Parameter(typeof(object), "target");
            var value = Expression.Parameter(typeof(object), "value");
            return Expression.Lambda<Action<object, object>>(
                Expression.Call(
                    Expression.Convert(target, type),
                    prop.GetSetMethod()!,
                    Expression.Convert(value, prop.PropertyType)),
                target, value).Compile();
        });
    }

    public TModel MapToModel<TModel>(object entity) =>
        MapToModel<TModel>(entity, typeof(TModel).Name);

    public TModel MapToModel<TModel>(object entity, string alias)
    {
        Console.WriteLine($"MapToModel called: {typeof(TModel).Name} with alias '{alias}'");

        if (!_mappings.TryGetValue(alias, out var nodeMap))
        {
            Console.WriteLine($"No NodeMap found for alias '{alias}'");
            return default!;
        }

        var model = Activator.CreateInstance<TModel>()!;
        var entityType = entity.GetType();

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            var entityPropName =
                nodeMap.EntityProperties.ContainsKey(fieldMap.DestinationName)
                    ? fieldMap.DestinationName
                    : fieldMap.SourceName;

            var value = GetCachedGetter(entityType, entityPropName)(entity);
            Console.WriteLine($"Field '{fieldMap.SourceName}' <- EntityProp '{entityPropName}', Value: {value}");

            if (value == null) continue;
            if (nodeMap.ModelProperties.ContainsKey(fieldMap.SourceName))
                GetCachedSetter(nodeMap.ModelType, fieldMap.SourceName)(model, value!);
        }

        return model;
    }

    private object MapNested(object entity, string alias, Type targetType)
    {
        var method = typeof(Mapper)
            .GetMethod(nameof(MapToModel), new[] { typeof(object), typeof(string) })!
            .MakeGenericMethod(targetType);
        return method.Invoke(this, new object?[] { entity, alias })!;
    }

    public object MapByAlias(Type entityType, object entity, string alias)
    {
        if (entity == null) return null!;
        if (!_mappings.TryGetValue(alias, out var nodeMap)) return null!;
        var model = Activator.CreateInstance(nodeMap.ModelType)!;
        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            var entityPropName =
                nodeMap.EntityProperties.ContainsKey(fieldMap.DestinationName)
                    ? fieldMap.DestinationName
                    : fieldMap.SourceName;

            var value = GetCachedGetter(entityType, entityPropName)(entity);
            if (value == null) continue;
            if (nodeMap.ModelProperties.ContainsKey(fieldMap.SourceName))
                GetCachedSetter(nodeMap.ModelType, fieldMap.SourceName)(model, value!);
        }
        return model;
    }

    public object MapToEntity<TModel>(TModel model) => throw new NotImplementedException();

    public TModel MapToUpdatedModel<TModel, TEntity, TKey>(
        TModel current,
        TEntity from,
        Func<TModel, TKey> modelKeySelector,
        params object[] nestedEntities)
        where TModel : class
        where TEntity : class
    {
        throw new NotImplementedException();
    }
}