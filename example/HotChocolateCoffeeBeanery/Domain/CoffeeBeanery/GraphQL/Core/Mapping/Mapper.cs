using System.Collections;
using System.Collections.Concurrent;
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

    public TModel MapToModel<TModel>(object entity) where TModel : class
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

    public object MapToEntity<TModel>(TModel model) where TModel : class
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
        if (source == null)     throw new ArgumentNullException(nameof(source));
        if (targetType == null) throw new ArgumentNullException(nameof(targetType));
        if (current == null)    throw new ArgumentNullException(nameof(current));
        if (string.IsNullOrWhiteSpace(idPropertyName))
            throw new ArgumentNullException(nameof(idPropertyName));

        var sourceModelType    = ((object)source).GetType();
        var incomingEntityType = current.GetType();

        NodeMap nodeMap;
        if (mappingAlias != null)
        {
            if (!_mappings.TryGetValue(mappingAlias, out nodeMap))
            {
                nodeMap = _mappings.Values.FirstOrDefault(m =>
                              m.EntityType == incomingEntityType &&
                              m.ModelType  == targetType)
                          ?? _mappings.Values.FirstOrDefault(m =>
                              m.EntityType?.Name.Equals(
                                  incomingEntityType.Name,
                                  StringComparison.OrdinalIgnoreCase) == true)
                          ?? throw new InvalidOperationException(
                              $"No mapping found for alias '{mappingAlias}' or entity " +
                              $"'{incomingEntityType.Name}' / model '{targetType.Name}'. " +
                              $"Registered keys: [{string.Join(", ", _mappings.Keys)}]");
            }
        }
        else
        {
            nodeMap = _mappings.Values
                          .FirstOrDefault(m =>
                              m.EntityType == incomingEntityType &&
                              m.ModelType  == targetType)
                      ?? _mappings.Values.FirstOrDefault(m =>
                          m.EntityType?.Name.Equals(
                              incomingEntityType.Name,
                              StringComparison.OrdinalIgnoreCase) == true)
                      ?? throw new InvalidOperationException(
                          $"No mapping found for entity '{incomingEntityType.Name}' " +
                          $"and model '{targetType.Name}'. " +
                          $"Consider passing a mappingAlias explicitly.");
        }

        var idFieldMap = nodeMap.FieldMaps
            .FirstOrDefault(f =>
                f.DestinationName.Equals(idPropertyName, StringComparison.OrdinalIgnoreCase));

        if (idFieldMap == null)
            throw new InvalidOperationException(
                $"No FieldMap found with DestinationName '{idPropertyName}' " +
                $"in mapping '{nodeMap.Alias}'.");

        var modelIdPropertyName = idFieldMap.SourceName;

        var incomingIdProp = incomingEntityType
            .GetProperty(idPropertyName, BindingFlags.Public | BindingFlags.Instance);

        if (incomingIdProp == null)
            throw new InvalidOperationException(
                $"Property '{idPropertyName}' not found on '{incomingEntityType.Name}'.");

        var incomingIdValue = incomingIdProp.GetValue(current);
        var mappedModel = Activator.CreateInstance(targetType)!;

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            if (!string.IsNullOrEmpty(fieldMap.DestinationEntity) &&
                !fieldMap.DestinationEntity.Equals(incomingEntityType.Name,
                    StringComparison.OrdinalIgnoreCase))
                continue;

            var sourceProp = incomingEntityType
                .GetProperty(fieldMap.DestinationName, BindingFlags.Public | BindingFlags.Instance);
            if (sourceProp == null) continue;

            var value = sourceProp.GetValue(current);
            if (value == null) continue;

            if (fieldMap.ToEnum is { Count: > 0 })
            {
                var valueStr = value.ToString()!;
                if (fieldMap.ToEnum.TryGetValue(valueStr, out var enumInt))
                    value = enumInt;
                else if (int.TryParse(valueStr, out var intVal) &&
                         fieldMap.ToEnum.Values.Cast<int?>()
                             .FirstOrDefault(v => v == intVal) is { } matched)
                    value = matched;
            }

            var destProp = targetType
                .GetProperty(fieldMap.SourceName, BindingFlags.Public | BindingFlags.Instance);

            if (destProp == null || !destProp.CanWrite) continue;

            var actualType = Nullable.GetUnderlyingType(destProp.PropertyType)
                             ?? destProp.PropertyType;

            if (actualType.IsEnum && value is int intValue)
                value = Enum.ToObject(actualType, intValue);
            
            if (value != null && !actualType.IsAssignableFrom(value.GetType()))
            {
                try { value = Convert.ChangeType(value, actualType); }
                catch { continue; }
            }

            destProp.SetValue(mappedModel, value);
        }

        var mergedModel = Activator.CreateInstance(sourceModelType)!;
        foreach (var prop in sourceModelType
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.CanRead && p.CanWrite))
        {
            prop.SetValue(mergedModel, prop.GetValue(source));
        }
        
        var listProperty = sourceModelType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p =>
                p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
                p.PropertyType.GetGenericArguments()[0] == targetType);

        if (listProperty != null)
        {
            var existingList = listProperty.GetValue(mergedModel);
            if (existingList == null)
            {
                existingList = Activator.CreateInstance(listProperty.PropertyType)!;
                listProperty.SetValue(mergedModel, existingList);
            }

            var list = (IList)existingList;

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

            if (existingIndex >= 0)
                list[existingIndex] = mappedModel;
            else
                list.Add(mappedModel);
        }
        else
        {
            foreach (var fieldMap in nodeMap.FieldMaps)
            {
                var mappedProp = targetType
                    .GetProperty(fieldMap.SourceName, BindingFlags.Public | BindingFlags.Instance);
                if (mappedProp == null) continue;

                var mappedValue  = mappedProp.GetValue(mappedModel);
                var defaultValue = mappedProp.PropertyType.IsValueType
                    ? Activator.CreateInstance(mappedProp.PropertyType)
                    : null;

                if (Equals(mappedValue, defaultValue)) continue;

                var mergedProp = sourceModelType
                    .GetProperty(fieldMap.SourceName, BindingFlags.Public | BindingFlags.Instance);
                if (mergedProp == null || !mergedProp.CanWrite) continue;

                if (mappedValue != null)
                {
                    var mergedActualType = Nullable.GetUnderlyingType(mergedProp.PropertyType)
                                           ?? mergedProp.PropertyType;
                    if (!mergedActualType.IsAssignableFrom(mappedValue.GetType()))
                    {
                        try { mappedValue = Convert.ChangeType(mappedValue, mergedActualType); }
                        catch { continue; }
                    }
                }

                mergedProp.SetValue(mergedModel, mappedValue);
            }
        }

        return mergedModel;
    }

    private void MapProperties(object entity, object model, NodeMap nodeMap)
    {
        var entityType     = entity.GetType();
        var entityTypeName = entityType.Name;
        var modelType      = model.GetType();

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            if (!string.IsNullOrEmpty(fieldMap.DestinationEntity) &&
                !fieldMap.DestinationEntity.Equals(entityTypeName,
                    StringComparison.OrdinalIgnoreCase))
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

    private object MergeIntoExisting(object existing, object entity, NodeMap nodeMap)
    {
        var entityType = entity.GetType();
        var modelType  = existing.GetType();

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            if (!string.IsNullOrEmpty(fieldMap.DestinationEntity) &&
                !fieldMap.DestinationEntity.Equals(entityType.Name,
                    StringComparison.OrdinalIgnoreCase))
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