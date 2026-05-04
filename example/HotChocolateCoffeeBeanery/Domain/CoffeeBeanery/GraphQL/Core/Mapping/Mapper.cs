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
    private readonly ConcurrentDictionary<string, PropertyInfo[]> _propCache  = new();
    private readonly ConcurrentDictionary<string, Func<object, object>> _getterCache = new();
    private readonly ConcurrentDictionary<string, Action<object, object>> _setterCache = new();

    public Mapper(Dictionary<string, NodeMap> mappings)
    {
        _mappings = mappings;
    }

    // ── Property cache helpers ────────────────────────────────────────────────

    private PropertyInfo[] GetCachedProperties(Type type) =>
        _propCache.GetOrAdd(type.FullName!, _ =>
            type.GetProperties(BindingFlags.Public | BindingFlags.Instance));

    private Func<object, object> GetCachedGetter(Type type, string propName)
    {
        var key = $"{type.FullName}.{propName}";
        return _getterCache.GetOrAdd(key, _ =>
        {
            var prop = type.GetProperty(propName,
                BindingFlags.Public | BindingFlags.Instance);
            if (prop?.GetGetMethod() == null) return _ => null!;
            var target = Expression.Parameter(typeof(object), "target");
            return Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Call(Expression.Convert(target, type), prop.GetGetMethod()!),
                    typeof(object)),
                target).Compile();
        });
    }

    private Action<object, object> GetCachedSetter(Type type, string propName)
    {
        var key = $"{type.FullName}.{propName}";
        return _setterCache.GetOrAdd(key, _ =>
        {
            var prop = type.GetProperty(propName,
                BindingFlags.Public | BindingFlags.Instance);
            if (prop?.GetSetMethod() == null) return (_, __) => { };
            var target = Expression.Parameter(typeof(object), "target");
            var value  = Expression.Parameter(typeof(object), "value");
            return Expression.Lambda<Action<object, object>>(
                Expression.Call(
                    Expression.Convert(target, type),
                    prop.GetSetMethod()!,
                    Expression.Convert(value, prop.PropertyType)),
                target, value).Compile();
        });
    }

    // ── Public API ────────────────────────────────────────────────────────────

    public TModel MapToModel<TModel>(object entity)
    {
        if (entity == null) return default!;
        var modelType = typeof(TModel);
        if (!_mappings.TryGetValue(modelType.Name, out var nodeMap))
            throw new InvalidOperationException(
                $"No mapping registered for '{modelType.Name}'");

        var model = Activator.CreateInstance<TModel>()!;
        MapProperties(entity, model, nodeMap);
        return model;
    }

    public object MapToEntity<TModel>(TModel model)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Map a single entity object to its model using the registration identified
    /// by <paramref name="alias"/>.  Only FieldMaps whose DestinationEntity matches
    /// the runtime type of <paramref name="entity"/> are applied, so cross-entity
    /// mappings (e.g. Product spanning Contract + Account + Transaction) work correctly
    /// when each entity arrives separately.
    /// </summary>
    public object MapByAlias(Type entityType, object entity, string alias)
    {
        if (entity == null) return null!;
        if (!_mappings.TryGetValue(alias, out var nodeMap))
            throw new InvalidOperationException(
                $"No mapping registered for alias '{alias}'");

        var model = Activator.CreateInstance(nodeMap.ModelType)!;
        MapProperties(entity, model, nodeMap);
        return model;
    }

    public TModel MapToUpdatedModel<TModel, TEntity, TKey>(
        TModel current, TEntity from,
        Func<TModel, TKey> modelKeySelector,
        params object[] nestedEntities)
        where TModel : class
        where TEntity : class
    {
        throw new NotImplementedException();
    }

    public object MapDynamicToModel(
        object source, Type targetType, object current, string idPropertyName)
    {
        if (source == null || current == null)
            return current ?? Activator.CreateInstance(targetType)!;

        var target = current ?? Activator.CreateInstance(targetType)!;
        var props  = GetCachedProperties(targetType);

        foreach (var targetProp in props)
        {
            var value = GetCachedGetter(source.GetType(), targetProp.Name)(source);
            if (value == null) continue;

            var actualType = Nullable.GetUnderlyingType(targetProp.PropertyType)
                             ?? targetProp.PropertyType;

            if (actualType.IsPrimitive || actualType == typeof(string)
                                       || actualType.IsEnum || actualType == typeof(Guid))
            {
                targetProp.SetValue(target, value);
                continue;
            }

            if (!_mappings.TryGetValue(actualType.Name, out _))
            {
                targetProp.SetValue(target, value);
                continue;
            }

            if (typeof(IEnumerable).IsAssignableFrom(actualType) && actualType.IsGenericType)
            {
                var list     = Activator.CreateInstance(targetProp.PropertyType) as IList;
                var itemType = targetProp.PropertyType.GetGenericArguments()[0];
                foreach (var item in (IEnumerable)value)
                    list!.Add(MapDynamicToModel(item, itemType, null!, targetProp.Name));
                targetProp.SetValue(target, list);
            }
            else
            {
                var nested = MapDynamicToModel(
                    value, actualType, targetProp.GetValue(target)!, targetProp.Name);
                targetProp.SetValue(target, nested);
            }
        }

        return target;
    }

    // ── Core mapping logic ────────────────────────────────────────────────────

    /// <summary>
    /// Apply FieldMaps from <paramref name="nodeMap"/> to <paramref name="model"/>,
    /// reading values from <paramref name="entity"/>.
    ///
    /// KEY FIX: only apply a FieldMap when the entity's runtime type name matches
    /// FieldMap.DestinationEntity.  This handles cross-entity model mappings such as
    /// ProductMapping (fields from Contract, Account, Transaction, CustomerBankingRelationship)
    /// where each call supplies one concrete entity — fields belonging to other entities
    /// are simply skipped rather than returning null and stomping existing values.
    /// </summary>
    private void MapProperties(object entity, object model, NodeMap nodeMap)
    {
        var entityType     = entity.GetType();
        var entityTypeName = entityType.Name;
        var modelType      = model.GetType();

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            // Skip fields that belong to a different entity than the one we have.
            // DestinationEntity is the authoritative entity name set in every FieldMap.
            if (!string.IsNullOrEmpty(fieldMap.DestinationEntity) &&
                !fieldMap.DestinationEntity.Equals(entityTypeName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            // Read from entity using DestinationName (the entity property name)
            var value = GetCachedGetter(entityType, fieldMap.DestinationName)(entity);
            if (value == null) continue;

            // Write to model using SourceName (the model property name)
            var targetProp = modelType.GetProperty(fieldMap.SourceName,
                BindingFlags.Public | BindingFlags.Instance);
            if (targetProp == null) continue;

            var actualType = Nullable.GetUnderlyingType(targetProp.PropertyType)
                             ?? targetProp.PropertyType;

            if (actualType.IsPrimitive || actualType == typeof(string) || actualType == typeof(Guid))
            {
                targetProp.SetValue(model, value);
            }
            else if (actualType.IsEnum)
            {
                // Honour ToEnum mapping when present
                if (fieldMap.ToEnum != null && fieldMap.ToEnum.Count > 0)
                {
                    var valueStr = value.ToString()!;
                    // Try direct name match first
                    if (fieldMap.ToEnum.TryGetValue(valueStr, out var enumInt))
                    {
                        targetProp.SetValue(model, Enum.ToObject(actualType, enumInt));
                        continue;
                    }
                    // Try matching by integer value
                    if (int.TryParse(valueStr, out var intVal))
                    {
                        var match = fieldMap.ToEnum.Values
                            .Cast<int?>()
                            .FirstOrDefault(v => v == intVal);
                        if (match.HasValue)
                        {
                            targetProp.SetValue(model, Enum.ToObject(actualType, match.Value));
                            continue;
                        }
                    }
                }

                // Fallback: parse directly
                var enumValue = value is string s
                    ? Enum.Parse(actualType, s, ignoreCase: true)
                    : Enum.ToObject(actualType, value);

                targetProp.SetValue(model, enumValue);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(actualType)
                     && actualType.IsGenericType)
            {
                var list     = Activator.CreateInstance(targetProp.PropertyType) as IList;
                var itemType = targetProp.PropertyType.GetGenericArguments()[0];
                foreach (var item in (IEnumerable)value)
                    list!.Add(MapDynamicToModel(item, itemType, null!, targetProp.Name));
                targetProp.SetValue(model, list);
            }
            else
            {
                var nested = MapDynamicToModel(
                    value, actualType, targetProp.GetValue(model)!, targetProp.Name);
                targetProp.SetValue(model, nested);
            }
        }
    }
}