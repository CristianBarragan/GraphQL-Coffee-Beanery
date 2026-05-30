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
    object MapToEntity<TModel>(TModel model) where TModel : class;
    TModel MapToModel<TModel>(object entity) where TModel : class;
    object MapByAlias(Type entityType, object entity, string alias);
    TModel MapToUpdatedModel<TModel, TEntity, TKey>(
        TModel current,
        TEntity from,
        Func<TModel, TKey> modelKeySelector,
        params object[] nestedEntities)
        where TModel : class
        where TEntity : class;
    object MapDynamicToModel(
        dynamic source,
        Type targetType,
        object current,
        string idPropertyName,
        string? mappingAlias = null);
}

public class Mapper : IMapper
{
    private readonly Dictionary<string, NodeMap> _mappings;
    private readonly ConcurrentDictionary<string, PropertyInfo[]> _propCache   = new();
    private readonly ConcurrentDictionary<string, Func<object, object>>   _getterCache = new();
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
            var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
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
            var prop = type.GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
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

    public TModel MapToModel<TModel>(object entity) where TModel : class // FIXED: added constraint
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

    public object MapToEntity<TModel>(TModel model) where TModel : class // FIXED: added constraint
    {
        throw new NotImplementedException();
    }

    public object MapByAlias(Type entityType, object entity, string alias)
    {
        if (entity == null) return null!;
        if (!_mappings.TryGetValue(alias, out var nodeMap))
            throw new InvalidOperationException(
                $"No mapping registered for alias '{alias}'");

        var mapped = Activator.CreateInstance(nodeMap.ModelType)!;
        MapProperties(entity, mapped, nodeMap);
        return mapped;
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
        dynamic source,
        Type targetType,
        object current,
        string idPropertyName,
        string? mappingAlias = null)
    {
        if (source == null)          throw new ArgumentNullException(nameof(source));
        if (targetType == null)      throw new ArgumentNullException(nameof(targetType));
        if (current == null)         throw new ArgumentNullException(nameof(current));
        if (string.IsNullOrWhiteSpace(idPropertyName))
            throw new ArgumentNullException(nameof(idPropertyName));

        var sourceModelType    = ((object)source).GetType();
        var incomingEntityType = current.GetType();

        // Step 1 — resolve NodeMap
        NodeMap nodeMap;
        if (mappingAlias != null)
        {
            if (!_mappings.TryGetValue(mappingAlias, out nodeMap))
                throw new InvalidOperationException(
                    $"No mapping found for alias '{mappingAlias}'.");
        }
        else
        {
            nodeMap = _mappings.Values
                .FirstOrDefault(m =>
                    m.EntityType == incomingEntityType &&
                    m.ModelType  == targetType)
                ?? throw new InvalidOperationException(
                    $"No mapping found for entity '{incomingEntityType.Name}' " +
                    $"and model '{targetType.Name}'. " +
                    $"Consider passing a mappingAlias explicitly.");
        }

        // Step 2 — infer model-side ID property name from FieldMaps
        var idFieldMap = nodeMap.FieldMaps
            .FirstOrDefault(f =>
                f.DestinationName.Equals(idPropertyName, StringComparison.OrdinalIgnoreCase));

        if (idFieldMap == null)
            throw new InvalidOperationException(
                $"No FieldMap found with DestinationName '{idPropertyName}' " +
                $"in mapping '{nodeMap.Alias}'.");

        var modelIdPropertyName = idFieldMap.SourceName;

        // Step 3 — read unique ID value from incoming entity
        var incomingIdProp = incomingEntityType
            .GetProperty(idPropertyName, BindingFlags.Public | BindingFlags.Instance);

        if (incomingIdProp == null)
            throw new InvalidOperationException(
                $"Property '{idPropertyName}' not found on '{incomingEntityType.Name}'.");

        var incomingIdValue = incomingIdProp.GetValue(current);

        // Step 4 — map incoming entity to targetType via FieldMaps
        var mappedModel = Activator.CreateInstance(targetType)!;

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            // Skip fields belonging to a different entity
            if (!string.IsNullOrEmpty(fieldMap.DestinationEntity) &&
                !fieldMap.DestinationEntity.Equals(incomingEntityType.Name,
                    StringComparison.OrdinalIgnoreCase))
                continue;

            var sourceProp = incomingEntityType
                .GetProperty(fieldMap.DestinationName, BindingFlags.Public | BindingFlags.Instance);
            if (sourceProp == null) continue;

            var value = sourceProp.GetValue(current);
            if (value == null) continue;

            // FIXED: use fieldMap.ToEnum (Dictionary<string,int> on FieldMap, not NodeMap)
            if (fieldMap.ToEnum is { Count: > 0 })
            {
                var valueStr = value.ToString()!;
                if (fieldMap.ToEnum.TryGetValue(valueStr, out var enumInt))
                    value = enumInt;
                else if (int.TryParse(valueStr, out var intVal) &&
                         fieldMap.ToEnum.Values.Cast<int?>().FirstOrDefault(v => v == intVal)
                             is { } matched)
                    value = matched;
            }

            if (!nodeMap.ModelProperties.TryGetValue(fieldMap.SourceName, out var destProp))
                continue;

            var actualType = Nullable.GetUnderlyingType(destProp.PropertyType)
                             ?? destProp.PropertyType;

            // Coerce int → enum on model side when needed
            if (actualType.IsEnum && value is int intValue)
                value = Enum.ToObject(actualType, intValue);

            destProp.SetValue(mappedModel, value);
        }

        // Step 5 — clone source TModel as base
        var mergedModel = Activator.CreateInstance(sourceModelType)!;
        foreach (var prop in sourceModelType
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.CanRead && p.CanWrite))
        {
            prop.SetValue(mergedModel, prop.GetValue(source));
        }

        // Step 6 — find List<targetType> on source TModel
        var listProperty = sourceModelType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p =>
                p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
                p.PropertyType.GetGenericArguments()[0] == targetType);

        if (listProperty != null)
        {
            // Step 7a — get or initialise the List<T>
            var existingList = listProperty.GetValue(mergedModel);
            if (existingList == null)
            {
                existingList = Activator.CreateInstance(listProperty.PropertyType)!;
                listProperty.SetValue(mergedModel, existingList);
            }

            var list = (IList)existingList;

            // Step 8 — find existing item by model-side ID
            var modelIdProp = targetType
                .GetProperty(modelIdPropertyName, BindingFlags.Public | BindingFlags.Instance);

            var existingIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                if (Equals(modelIdProp?.GetValue(list[i]), incomingIdValue))
                {
                    existingIndex = i;
                    break;
                }
            }

            // Step 9 — upsert
            if (existingIndex >= 0)
                list[existingIndex] = mappedModel;
            else
                list.Add(mappedModel);
        }
        else
        {
            // Step 7b — no List<T>: overlay non-default FieldMap fields onto mergedModel
            foreach (var fieldMap in nodeMap.FieldMaps)
            {
                if (!nodeMap.ModelProperties.TryGetValue(fieldMap.SourceName, out var mappedProp))
                    continue;

                var mappedValue  = mappedProp.GetValue(mappedModel);
                var defaultValue = mappedProp.PropertyType.IsValueType
                    ? Activator.CreateInstance(mappedProp.PropertyType)
                    : null;

                if (Equals(mappedValue, defaultValue)) continue;

                sourceModelType
                    .GetProperty(fieldMap.SourceName, BindingFlags.Public | BindingFlags.Instance)
                    ?.SetValue(mergedModel, mappedValue);
            }
        }

        return mergedModel;
    }

    // ── Core mapping logic ────────────────────────────────────────────────────

    private void MapProperties(object entity, object model, NodeMap nodeMap)
    {
        var entityType     = entity.GetType();
        var entityTypeName = entityType.Name;
        var modelType      = model.GetType();

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            if (!string.IsNullOrEmpty(fieldMap.DestinationEntity) &&
                !fieldMap.DestinationEntity.Equals(entityTypeName, StringComparison.OrdinalIgnoreCase))
                continue;

            var value = GetCachedGetter(entityType, fieldMap.DestinationName)(entity);
            if (value == null) continue;

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
                // FIXED: fieldMap.ToEnum (Dictionary<string,int>), not nodeMap.ToEnum
                if (fieldMap.ToEnum is { Count: > 0 })
                {
                    var valueStr = value.ToString()!;
                    if (fieldMap.ToEnum.TryGetValue(valueStr, out var enumInt))
                    {
                        targetProp.SetValue(model, Enum.ToObject(actualType, enumInt));
                        continue;
                    }
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

                var enumValue = value is string s
                    ? Enum.Parse(actualType, s, ignoreCase: true)
                    : Enum.ToObject(actualType, value);
                targetProp.SetValue(model, enumValue);
            }
            else if (typeof(IEnumerable).IsAssignableFrom(actualType) && actualType.IsGenericType)
            {
                var list     = Activator.CreateInstance(targetProp.PropertyType) as IList;
                var itemType = targetProp.PropertyType.GetGenericArguments()[0];
                foreach (var item in (IEnumerable)value)
                    list!.Add(MapByAlias(itemType, item, nodeMap.Alias));
                targetProp.SetValue(model, list);
            }
            else
            {
                var existing = targetProp.GetValue(model);
                var nested   = existing != null
                    ? MergeIntoExisting(existing, value, nodeMap)
                    : MapByAlias(actualType, value, nodeMap.Alias);
                targetProp.SetValue(model, nested);
            }
        }
    }

    // Overlays non-null FieldMap values from entity onto an existing model instance
    private object MergeIntoExisting(object existing, object entity, NodeMap nodeMap)
    {
        var entityType = entity.GetType();
        var modelType  = existing.GetType();

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            if (!string.IsNullOrEmpty(fieldMap.DestinationEntity) &&
                !fieldMap.DestinationEntity.Equals(entityType.Name, StringComparison.OrdinalIgnoreCase))
                continue;

            var value = GetCachedGetter(entityType, fieldMap.DestinationName)(entity);
            if (value == null) continue;

            modelType
                .GetProperty(fieldMap.SourceName, BindingFlags.Public | BindingFlags.Instance)
                ?.SetValue(existing, value);
        }

        return existing;
    }
}