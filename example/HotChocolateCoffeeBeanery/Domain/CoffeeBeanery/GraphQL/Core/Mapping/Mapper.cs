using System;
using System.Linq;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Mapping;

public interface IMapper
{
    object MapToEntity<TModel>(TModel model);
    TModel MapToModel<TModel>(object entity);

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
    private readonly Dictionary<string, NodeMap> _mappings = new Dictionary<string, NodeMap>();

    public Mapper(Dictionary<string, NodeMap> mappings)
    {
        _mappings = mappings;
    }

    public object MapToEntity<TModel>(TModel model)
    {
        if (model == null) return null;

        var nodeMap = _mappings[typeof(TModel).Name];
        var entity = Activator.CreateInstance(nodeMap.EntityType);

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            if (!nodeMap.ModelProperties.TryGetValue(fieldMap.SourceName, out var sourceProp))
                continue;

            var value = sourceProp.GetValue(model);

            if (value != null && nodeMap.FromEnum != null && sourceProp.PropertyType.IsEnum)
                value = nodeMap.FromEnum[value.ToString()];

            if (nodeMap.EntityProperties.TryGetValue(fieldMap.DestinationName, out var destProp))
                destProp.SetValue(entity, value);
        }

        return entity;
    }

    public TModel MapToModel<TModel>(object entity)
    {
        if (entity == null) return default(TModel);

        var modelType = typeof(TModel);
        var nodeMap = _mappings[modelType.Name];

        if (nodeMap == null)
            throw new InvalidOperationException($"No mapping registered for {modelType.Name}");

        var model = Activator.CreateInstance<TModel>();
        var entityType = entity.GetType();

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            var sourceProp = entityType.GetProperty(fieldMap.DestinationName);
            if (sourceProp == null) continue;

            var value = sourceProp.GetValue(entity);

            if (value != null && nodeMap.ToEnum != null && sourceProp.PropertyType.IsEnum)
                value = nodeMap.ToEnum[value.ToString()];

            var destProp = modelType.GetProperty(fieldMap.SourceName);
            if (destProp != null)
                destProp.SetValue(model, value);
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
        if (current == null) throw new ArgumentNullException(nameof(current));
        if (from == null) throw new ArgumentNullException(nameof(from));
        if (modelKeySelector == null) throw new ArgumentNullException(nameof(modelKeySelector));

        var modelType = typeof(TModel);
        var entityType = typeof(TEntity);

        // Identify the target TModel via the key selector
        var id = modelKeySelector(current);

        if (!_mappings.TryGetValue(modelType.Name, out var nodeMap))
            throw new InvalidOperationException(
                $"No mapping registered for {modelType.Name}.");

        // Clone current TModel as base so we only overwrite FieldMaps-defined fields
        var updatedModel = ShallowClone(current);

        // 1. Map flat fields from TEntity onto TModel via FieldMaps
        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            var sourceProp = entityType.GetProperty(fieldMap.DestinationName);
            if (sourceProp == null) continue;

            var value = sourceProp.GetValue(from);

            if (value != null && nodeMap.ToEnum != null && sourceProp.PropertyType.IsEnum)
                value = nodeMap.ToEnum[value.ToString()];

            if (nodeMap.ModelProperties.TryGetValue(fieldMap.SourceName, out var destProp))
                destProp.SetValue(updatedModel, value);
        }

        // 2. Map nested inner objects from each additional entity in nestedEntities
        foreach (var nestedEntity in nestedEntities ?? Array.Empty<object>())
        {
            if (nestedEntity == null) continue;

            var nestedEntityType = nestedEntity.GetType();

            // Find the NodeMap registered for this nested entity type
            var nestedNodeMap = _mappings.Values
                .FirstOrDefault(m => m.EntityType == nestedEntityType);

            if (nestedNodeMap == null) continue;

            // Find the matching nested property on TModel (e.g. CustomerCustomerEdge.Customer)
            var nestedModelProperty = modelType
                .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(p => _mappings.ContainsKey(p.PropertyType.Name)
                    && _mappings[p.PropertyType.Name].EntityType == nestedEntityType);

            if (nestedModelProperty == null) continue;

            // Instantiate the nested model and map fields from the nested entity
            var nestedModel = Activator.CreateInstance(nestedModelProperty.PropertyType);

            foreach (var fieldMap in nestedNodeMap.FieldMaps)
            {
                var sourceProp = nestedEntityType.GetProperty(fieldMap.DestinationName);
                if (sourceProp == null) continue;

                var value = sourceProp.GetValue(nestedEntity);

                if (value != null && nestedNodeMap.ToEnum != null && sourceProp.PropertyType.IsEnum)
                    value = nestedNodeMap.ToEnum[value.ToString()];

                if (nestedNodeMap.ModelProperties.TryGetValue(fieldMap.SourceName, out var destProp))
                    destProp.SetValue(nestedModel, value);
            }

            // Set the populated nested model onto the updated TModel
            nestedModelProperty.SetValue(updatedModel, nestedModel);
        }

        return updatedModel;
    }
    
    public object MapDynamicToModel(
        dynamic source,
        Type targetType,
        object current,
        string idPropertyName)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        if (targetType == null) throw new ArgumentNullException(nameof(targetType));
        if (current == null) throw new ArgumentNullException(nameof(current));
        if (string.IsNullOrWhiteSpace(idPropertyName)) throw new ArgumentNullException(nameof(idPropertyName));

        var sourceModelType = ((object)source).GetType();
        var incomingEntityType = current.GetType();

        // Step 1 — resolve NodeMap for the incoming entity type
        if (!_mappings.TryGetValue(targetType.Name, out var nodeMap))
            throw new InvalidOperationException(
                $"No mapping registered for {targetType.Name}.");

        // Step 2 — infer the TModel property name from FieldMaps using idPropertyName
        // idPropertyName is on the entity side (DestinationName), we need the model side (SourceName)
        var idFieldMap = nodeMap.FieldMaps
            .FirstOrDefault(f => f.DestinationName.Equals(idPropertyName, StringComparison.OrdinalIgnoreCase));

        if (idFieldMap == null)
            throw new InvalidOperationException(
                $"No FieldMap found with DestinationName '{idPropertyName}' in mapping for {targetType.Name}.");

        var modelIdPropertyName = idFieldMap.SourceName;

        // Step 3 — read the unique ID value from the incoming entity via reflection
        var incomingIdProp = incomingEntityType
            .GetProperty(idPropertyName, BindingFlags.Public | BindingFlags.Instance);

        if (incomingIdProp == null)
            throw new InvalidOperationException(
                $"Property '{idPropertyName}' not found on incoming entity {incomingEntityType.Name}.");

        var incomingIdValue = incomingIdProp.GetValue(current);

        // Step 4 — fully map the incoming entity to its TModel type via FieldMaps
        var mappedModel = Activator.CreateInstance(targetType);

        foreach (var fieldMap in nodeMap.FieldMaps)
        {
            var sourceProp = incomingEntityType
                .GetProperty(fieldMap.DestinationName, BindingFlags.Public | BindingFlags.Instance);

            if (sourceProp == null) continue;

            var value = sourceProp.GetValue(current);

            if (value != null && nodeMap.ToEnum != null && sourceProp.PropertyType.IsEnum)
                value = nodeMap.ToEnum[value.ToString()];

            if (nodeMap.ModelProperties.TryGetValue(fieldMap.SourceName, out var destProp))
                destProp.SetValue(mappedModel, value);
        }

        // Step 5 — find the List<T> property on the source TModel that holds items of targetType
        var listProperty = sourceModelType
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .FirstOrDefault(p =>
                p.PropertyType.IsGenericType &&
                p.PropertyType.GetGenericTypeDefinition() == typeof(List<>) &&
                p.PropertyType.GetGenericArguments()[0] == targetType);

        // Step 6 — clone the source TModel as base
        var mergedModel = Activator.CreateInstance(sourceModelType);
        foreach (var prop in sourceModelType
                     .GetProperties(BindingFlags.Public | BindingFlags.Instance)
                     .Where(p => p.CanRead && p.CanWrite))
        {
            prop.SetValue(mergedModel, prop.GetValue(source));
        }

        if (listProperty != null)
        {
            // Step 7a — get or create the List<T> on the merged model
            var existingList = listProperty.GetValue(mergedModel);
            if (existingList == null)
            {
                existingList = Activator.CreateInstance(listProperty.PropertyType);
                listProperty.SetValue(mergedModel, existingList);
            }

            var list = existingList as System.Collections.IList;

            // Step 8 — find existing item in the list by the inferred model ID property
            var modelIdProp = targetType
                .GetProperty(modelIdPropertyName, BindingFlags.Public | BindingFlags.Instance);

            var existingIndex = -1;
            for (int i = 0; i < list.Count; i++)
            {
                var itemIdValue = modelIdProp?.GetValue(list[i]);
                if (Equals(itemIdValue, incomingIdValue))
                {
                    existingIndex = i;
                    break;
                }
            }

            // Step 9 — update existing item or add new one
            if (existingIndex >= 0)
                list[existingIndex] = mappedModel;
            else
                list.Add(mappedModel);
        }
        else
        {
            // Step 7b — no List<T> found, fall back to overlaying flat fields directly onto mergedModel
            foreach (var fieldMap in nodeMap.FieldMaps)
            {
                if (!nodeMap.ModelProperties.TryGetValue(fieldMap.SourceName, out var mappedProp))
                    continue;

                var mappedValue = mappedProp.GetValue(mappedModel);
                var defaultValue = mappedProp.PropertyType.IsValueType
                    ? Activator.CreateInstance(mappedProp.PropertyType)
                    : null;

                if (!Equals(mappedValue, defaultValue))
                {
                    var mergedProp = sourceModelType
                        .GetProperty(fieldMap.SourceName, BindingFlags.Public | BindingFlags.Instance);
                    mergedProp?.SetValue(mergedModel, mappedValue);
                }
            }
        }

        return mergedModel;
    }
    
    private TModel ShallowClone<TModel>(TModel source) where TModel : class
    {
        var memberwiseClone = typeof(object)
            .GetMethod("MemberwiseClone", BindingFlags.Instance | BindingFlags.NonPublic);

        return (TModel)memberwiseClone.Invoke(source, null);
    }
}