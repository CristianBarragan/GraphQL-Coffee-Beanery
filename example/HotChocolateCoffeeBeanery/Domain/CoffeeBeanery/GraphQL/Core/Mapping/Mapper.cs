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
    object MapToEntity<TModel>(TModel model);
    object MapByAlias(Type entityType, object entity, string alias);
    TModel MapToUpdatedModel<TModel, TEntity, TKey>(
        TModel current,
        TEntity from,
        Func<TModel, TKey> modelKeySelector,
        params object[] nestedEntities)
        where TModel : class
        where TEntity : class;
    object MapDynamicToModel(
        object source,
        Type targetType,
        object current,
        string idPropertyName);
}

public class Mapper : IMapper
{
    private readonly Dictionary<string, NodeMap> _mappings;
    private readonly ConcurrentDictionary<string, PropertyInfo[]> _propCache = new();
    private readonly ConcurrentDictionary<string, Func<object, object>> _getterCache = new();
    private readonly ConcurrentDictionary<string, Action<object, object>> _setterCache = new();
    private static readonly MethodInfo MemberwiseCloneMethod =
        typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public Mapper(Dictionary<string, NodeMap> mappings)
    {
        _mappings = mappings;
    }

    private PropertyInfo[] GetCachedProperties(Type type) =>
        _propCache.GetOrAdd(type.FullName!, _ => type.GetProperties(BindingFlags.Public | BindingFlags.Instance));

    private Func<object, object> GetCachedGetter(Type type, string propName)
    {
        var key = $"{type.FullName}.{propName}";
        return _getterCache.GetOrAdd(key, _ =>
        {
            var prop = type.GetProperty(propName);
            if (prop?.GetGetMethod() == null) return _ => null!;
            var target = Expression.Parameter(typeof(object), "target");
            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(Expression.Call(Expression.Convert(target, type), prop.GetGetMethod()!), typeof(object)),
                target).Compile();
        });
    }

    private Action<object, object> GetCachedSetter(Type type, string propName)
    {
        var key = $"{type.FullName}.{propName}";
        return _setterCache.GetOrAdd(key, _ =>
        {
            var prop = type.GetProperty(propName);
            if (prop?.GetSetMethod() == null) return (_, __) => { };
            var target = Expression.Parameter(typeof(object), "target");
            var value = Expression.Parameter(typeof(object), "value");
            return Expression.Lambda<Action<object, object>>(
                Expression.Call(Expression.Convert(target, type), prop.GetSetMethod()!, Expression.Convert(value, prop.PropertyType)),
                target, value).Compile();
        });
    }

    public TModel MapToModel<TModel>(object entity)
    {
        if (entity == null) return default!;
        var modelType = typeof(TModel);
        if (!_mappings.TryGetValue(modelType.Name, out var nodeMap))
            throw new InvalidOperationException($"No mapping registered for {modelType.Name}");

        var model = Activator.CreateInstance<TModel>()!;
        MapProperties(entity, model, nodeMap);
        return model;
    }

    public object MapToEntity<TModel>(TModel model)
    {
        throw new NotImplementedException();
    }

    public object MapByAlias(Type entityType, object entity, string alias)
    {
        if (entity == null) return null!;
        if (!_mappings.TryGetValue(alias, out var nodeMap))
            throw new InvalidOperationException($"No mapping registered for alias '{alias}'");

        var model = Activator.CreateInstance(nodeMap.ModelType)!;
        MapProperties(entity, model, nodeMap);
        return model;
    }

    public TModel MapToUpdatedModel<TModel, TEntity, TKey>(TModel current, TEntity from, Func<TModel, TKey> modelKeySelector, params object[] nestedEntities)
        where TModel : class
        where TEntity : class
    {
        throw new NotImplementedException();
    }

    public object MapDynamicToModel(object source, Type targetType, object current, string idPropertyName)
    {
        if (source == null) return current ?? Activator.CreateInstance(targetType)!;

        var target = current ?? Activator.CreateInstance(targetType)!;

        var props = GetCachedProperties(targetType);
        foreach (var targetProp in props)
        {
            var entityPropName = targetProp.Name;
            var value = GetCachedGetter(source.GetType(), entityPropName)(source);
            if (value == null) continue;

            Type actualType = Nullable.GetUnderlyingType(targetProp.PropertyType) ?? targetProp.PropertyType;

            // Skip primitives, string, enums, Guid
            if (actualType.IsPrimitive || actualType == typeof(string) || actualType.IsEnum || actualType == typeof(Guid))
            {
                targetProp.SetValue(target, value);
                continue;
            }

            // Recurse only for complex objects
            if (!_mappings.TryGetValue(actualType.Name, out var nodeMap))
            {
                targetProp.SetValue(target, value); // fallback for unknown type
                continue;
            }

            if (typeof(IEnumerable).IsAssignableFrom(actualType) && actualType.IsGenericType)
            {
                var list = Activator.CreateInstance(targetProp.PropertyType) as IList;
                var itemType = targetProp.PropertyType.GetGenericArguments()[0];

                foreach (var item in value as IEnumerable)
                {
                    var mappedItem = MapDynamicToModel(item, itemType, null, targetProp.Name);
                    list.Add(mappedItem);
                }

                targetProp.SetValue(target, list);
            }
            else
            {
                var nested = MapDynamicToModel(value, actualType, targetProp.GetValue(target), targetProp.Name);
                targetProp.SetValue(target, nested);
            }
        }

        return target;
    }

    private void MapProperties(object entity, object model, NodeMap nodeMap)
    {
        var entityType = entity.GetType();
        var modelType = model.GetType();

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            var value = GetCachedGetter(entityType, fieldMap.DestinationName)(entity);
            if (value == null) continue;

            var targetProp = modelType.GetProperty(fieldMap.SourceName);
            if (targetProp == null) continue;

            Type actualType = Nullable.GetUnderlyingType(targetProp.PropertyType) ?? targetProp.PropertyType;

            // Assign primitives, strings, enums directly
            if (actualType.IsPrimitive || actualType == typeof(string) || actualType.IsEnum || actualType == typeof(Guid))
            {
                targetProp.SetValue(model, value);
            }
            else
            {
                // Recurse for nested complex objects
                if (targetProp.PropertyType.IsGenericType && typeof(IEnumerable).IsAssignableFrom(targetProp.PropertyType))
                {
                    var list = Activator.CreateInstance(targetProp.PropertyType) as IList;
                    var itemType = targetProp.PropertyType.GetGenericArguments()[0];

                    foreach (var item in value as IEnumerable)
                    {
                        var mappedItem = MapDynamicToModel(item, itemType, null, targetProp.Name);
                        list.Add(mappedItem);
                    }

                    targetProp.SetValue(model, list);
                }
                else
                {
                    var nested = MapDynamicToModel(value, actualType, targetProp.GetValue(model), targetProp.Name);
                    targetProp.SetValue(model, nested);
                }
            }
        }
    }
}