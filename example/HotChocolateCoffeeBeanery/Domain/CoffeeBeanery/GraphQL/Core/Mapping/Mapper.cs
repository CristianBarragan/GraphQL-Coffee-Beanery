using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Mapping;

public interface IMapper
{
    object MapToEntity<TModel>(TModel model);
    TModel MapToModel<TModel>(object entity);

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
        string idPropertyName);
}

public class Mapper : IMapper
{
    private readonly Dictionary<string, NodeMap> _mappings;

    // ─── Reflection caches ────────────────────────────────────────────────────

    // Raw PropertyInfo arrays keyed by type full name
    private readonly ConcurrentDictionary<string, PropertyInfo[]> _propCache = new();

    // Compiled getter delegates: "{TypeFullName}.{PropertyName}" → Func<object, object>
    private readonly ConcurrentDictionary<string, Func<object, object>> _getterCache = new();

    // Compiled setter delegates: "{TypeFullName}.{PropertyName}" → Action<object, object>
    private readonly ConcurrentDictionary<string, Action<object, object>> _setterCache = new();

    // MemberwiseClone method info — looked up once
    private static readonly MethodInfo MemberwiseCloneMethod =
        typeof(object).GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public Mapper(Dictionary<string, NodeMap> mappings)
    {
        _mappings = mappings;
    }

    // ─── Cache helpers ────────────────────────────────────────────────────────

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
            var getter = Expression.Lambda<Func<object, object>>(
                Expression.Convert(
                    Expression.Call(Expression.Convert(target, type), prop.GetGetMethod()!),
                    typeof(object)),
                target).Compile();

            return getter;
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
            var value  = Expression.Parameter(typeof(object), "value");
            var setter = Expression.Lambda<Action<object, object>>(
                Expression.Call(
                    Expression.Convert(target, type),
                    prop.GetSetMethod()!,
                    Expression.Convert(value, prop.PropertyType)),
                target, value).Compile();

            return setter;
        });
    }

    // ─── IsScalarType helper ──────────────────────────────────────────────────

    private static bool IsScalarOrNullable(Type t) =>
        t.IsPrimitive
        || t == typeof(string)
        || t == typeof(Guid)
        || t == typeof(Guid?)
        || t == typeof(decimal)
        || t == typeof(decimal?)
        || t == typeof(DateTime)
        || t == typeof(DateTime?)
        || t == typeof(DateTimeOffset)
        || t == typeof(DateTimeOffset?)
        || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));

    // ─── Public API ───────────────────────────────────────────────────────────

    public object MapToEntity<TModel>(TModel model)
    {
        if (model == null) return null!;

        var nodeMap = _mappings[typeof(TModel).Name];
        var entity  = Activator.CreateInstance(nodeMap.EntityType)!;
        var modelType = typeof(TModel);

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            if (!nodeMap.ModelProperties.TryGetValue(fieldMap.SourceName, out var sourceProp))
                continue;

            var value = GetCachedGetter(modelType, sourceProp.Name)(model!);

            if (value != null && nodeMap.FromEnum != null && sourceProp.PropertyType.IsEnum)
                value = nodeMap.FromEnum[value.ToString()!];

            if (nodeMap.EntityProperties.TryGetValue(fieldMap.DestinationName, out var destProp))
                GetCachedSetter(nodeMap.EntityType, destProp.Name)(entity, value!);
        }

        return entity;
    }

    public TModel MapToModel<TModel>(object entity)
    {
        if (entity == null) return default!;

        var modelType = typeof(TModel);

        if (!_mappings.TryGetValue(modelType.Name, out var nodeMap))
            throw new InvalidOperationException($"No mapping registered for {modelType.Name}");

        var model      = Activator.CreateInstance<TModel>()!;
        var entityType = entity.GetType();

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            var value = GetCachedGetter(entityType, fieldMap.DestinationName)(entity);
            if (value == null) continue;

            var sourceProp = GetCachedProperties(entityType)
                .FirstOrDefault(p => p.Name == fieldMap.DestinationName);

            if (value != null && nodeMap.ToEnum != null && sourceProp?.PropertyType.IsEnum == true)
                value = nodeMap.ToEnum[value.ToString()!];

            GetCachedSetter(modelType, fieldMap.SourceName)(model!, value!);
        }

        return model;
    }

    public TModel MapToUpdatedModel<TModel, TEntity, TKey>(
        TModel current,
        TEntity from,
        Func<TModel, TKey> modelKeySelector,
        params object[] nestedEntities)
        where TModel : class
        where TEntity : class
    {
        if (current == null)          throw new ArgumentNullException(nameof(current));
        if (from == null)             throw new ArgumentNullException(nameof(from));
        if (modelKeySelector == null) throw new ArgumentNullException(nameof(modelKeySelector));

        var modelType  = typeof(TModel);
        var entityType = typeof(TEntity);

        if (!_mappings.TryGetValue(modelType.Name, out var nodeMap))
            throw new InvalidOperationException($"No mapping registered for {modelType.Name}.");

        var updatedModel = ShallowClone(current);

        // 1. Map flat fields — only where DestinationEntity matches TEntity
        foreach (var fieldMap in nodeMap.FieldMaps
            .Where(f => f.DestinationEntity == entityType.Name))
        {
            var value = GetCachedGetter(entityType, fieldMap.DestinationName)(from);
            if (value == null) continue;

            var sourceProp = GetCachedProperties(entityType)
                .FirstOrDefault(p => p.Name == fieldMap.DestinationName);

            if (nodeMap.ToEnum != null && sourceProp?.PropertyType.IsEnum == true)
                value = nodeMap.ToEnum[value.ToString()!];

            if (nodeMap.ModelProperties.ContainsKey(fieldMap.SourceName))
                GetCachedSetter(modelType, fieldMap.SourceName)(updatedModel, value);
        }

        // 2. Map nested entities
        foreach (var nestedEntity in nestedEntities ?? Array.Empty<object>())
        {
            if (nestedEntity == null) continue;

            var nestedEntityType = nestedEntity.GetType();

            var nestedModelProperty = GetCachedProperties(modelType)
                .FirstOrDefault(p =>
                    _mappings.TryGetValue(p.PropertyType.Name, out var m) &&
                    m.EntityType == nestedEntityType);

            if (nestedModelProperty == null) continue;

            if (!_mappings.TryGetValue(nestedModelProperty.PropertyType.Name, out var nestedNodeMap))
                continue;

            var nestedModel = Activator.CreateInstance(nestedModelProperty.PropertyType)!;

            foreach (var fieldMap in nestedNodeMap.FieldMaps
                .Where(f => f.DestinationEntity == nestedEntityType.Name))
            {
                var value = GetCachedGetter(nestedEntityType, fieldMap.DestinationName)(nestedEntity);
                if (value == null) continue;

                var sourceProp = GetCachedProperties(nestedEntityType)
                    .FirstOrDefault(p => p.Name == fieldMap.DestinationName);

                if (nestedNodeMap.ToEnum != null && sourceProp?.PropertyType.IsEnum == true)
                    value = nestedNodeMap.ToEnum[value.ToString()!];

                if (nestedNodeMap.ModelProperties.ContainsKey(fieldMap.SourceName))
                    GetCachedSetter(nestedModelProperty.PropertyType, fieldMap.SourceName)(nestedModel, value);
            }

            GetCachedSetter(modelType, nestedModelProperty.Name)(updatedModel, nestedModel);
        }

        return updatedModel;
    }

    public object MapDynamicToModel(
        dynamic source,
        Type targetType,
        object current,
        string idPropertyName)
    {
        if (source == null)                            throw new ArgumentNullException(nameof(source));
        if (targetType == null)                        throw new ArgumentNullException(nameof(targetType));
        if (current == null)                           throw new ArgumentNullException(nameof(current));
        if (string.IsNullOrWhiteSpace(idPropertyName)) throw new ArgumentNullException(nameof(idPropertyName));

        var sourceModelType    = ((object)source).GetType();
        var incomingEntityType = current.GetType();

        if (!_mappings.TryGetValue(targetType.Name, out var nodeMap))
            throw new InvalidOperationException($"No mapping registered for {targetType.Name}.");

        var idFieldMap = nodeMap.FieldMaps
            .FirstOrDefault(f => f.DestinationName.Equals(idPropertyName, StringComparison.OrdinalIgnoreCase));

        if (idFieldMap == null)
            throw new InvalidOperationException(
                $"No FieldMap found with DestinationName '{idPropertyName}' in mapping for {targetType.Name}.");

        var modelIdPropertyName = idFieldMap.SourceName;
        var incomingIdValue     = GetCachedGetter(incomingEntityType, idPropertyName)(current);

        // Map incoming entity → model
        var mappedModel = Activator.CreateInstance(targetType)!;

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            var value = GetCachedGetter(incomingEntityType, fieldMap.DestinationName)(current);
            if (value == null) continue;

            var sourceProp = GetCachedProperties(incomingEntityType)
                .FirstOrDefault(p => p.Name == fieldMap.DestinationName);

            if (nodeMap.ToEnum != null && sourceProp?.PropertyType.IsEnum == true)
                value = nodeMap.ToEnum[value.ToString()!];

            if (nodeMap.ModelProperties.ContainsKey(fieldMap.SourceName))
                GetCachedSetter(targetType, fieldMap.SourceName)(mappedModel, value);
        }

        // Clone source model
        var mergedModel = Activator.CreateInstance(sourceModelType)!;
        foreach (var prop in GetCachedProperties(sourceModelType).Where(p => p.CanRead && p.CanWrite))
            GetCachedSetter(sourceModelType, prop.Name)(mergedModel,
                GetCachedGetter(sourceModelType, prop.Name)(source));

        // Find List<T> property on source model
        var listProperty = GetCachedProperties(sourceModelType)
            .FirstOrDefault(p =>
                p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
                p.PropertyType.GetGenericArguments()[0] == targetType);

        if (listProperty != null)
        {
            var existingList = GetCachedGetter(sourceModelType, listProperty.Name)(mergedModel);
            if (existingList == null)
            {
                existingList = Activator.CreateInstance(listProperty.PropertyType)!;
                GetCachedSetter(sourceModelType, listProperty.Name)(mergedModel, existingList);
            }

            var list       = (System.Collections.IList)existingList;
            var modelIdProp = GetCachedProperties(targetType)
                .FirstOrDefault(p => p.Name == modelIdPropertyName);

            var existingIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                var itemIdValue = modelIdProp != null
                    ? GetCachedGetter(targetType, modelIdProp.Name)(list[i]!)
                    : null;

                if (Equals(itemIdValue, incomingIdValue)) { existingIndex = i; break; }
            }

            if (existingIndex >= 0) list[existingIndex] = mappedModel;
            else                    list.Add(mappedModel);
        }
        else
        {
            foreach (var fieldMap in nodeMap.FieldMaps)
            {
                if (!nodeMap.ModelProperties.ContainsKey(fieldMap.SourceName)) continue;

                var mappedValue  = GetCachedGetter(targetType, fieldMap.SourceName)(mappedModel);
                var mappedProp   = GetCachedProperties(targetType).FirstOrDefault(p => p.Name == fieldMap.SourceName);
                var defaultValue = mappedProp?.PropertyType.IsValueType == true
                    ? Activator.CreateInstance(mappedProp.PropertyType)
                    : null;

                if (!Equals(mappedValue, defaultValue))
                    GetCachedSetter(sourceModelType, fieldMap.SourceName)(mergedModel, mappedValue!);
            }
        }

        return mergedModel;
    }

    public object MapByAlias(Type entityType, object entity, string alias)
    {
        if (entity == null)                           throw new ArgumentNullException(nameof(entity));
        if (entityType == null)                       throw new ArgumentNullException(nameof(entityType));
        if (string.IsNullOrWhiteSpace(alias))         throw new ArgumentNullException(nameof(alias));

        if (!_mappings.TryGetValue(alias, out var nodeMap) &&
            !_mappings.TryGetValue(entityType.Name, out nodeMap))
            throw new InvalidOperationException(
                $"No mapping registered for alias '{alias}' or type '{entityType.Name}'.");

        // Always create from the registered ModelType to avoid Domain vs Entity mismatch
        var mappedModel = Activator.CreateInstance(nodeMap.ModelType)!;

        foreach (var fieldMap in nodeMap.FieldMaps
            .Where(f => f.DestinationEntity == entityType.Name))
        {
            var value = GetCachedGetter(entityType, fieldMap.DestinationName)(entity);
            if (value == null) continue;

            var sourceProp = GetCachedProperties(entityType)
                .FirstOrDefault(p => p.Name == fieldMap.DestinationName);

            if (nodeMap.ToEnum != null && sourceProp?.PropertyType.IsEnum == true)
                value = nodeMap.ToEnum[value.ToString()!];

            if (nodeMap.ModelProperties.ContainsKey(fieldMap.SourceName))
                GetCachedSetter(nodeMap.ModelType, fieldMap.SourceName)(mappedModel, value);
        }

        return mappedModel;
    }

    // ─── Private helpers ──────────────────────────────────────────────────────

    private TModel ShallowClone<TModel>(TModel source) where TModel : class =>
        (TModel)MemberwiseCloneMethod.Invoke(source, null)!;
}