using System;
using System.Linq;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Mapping;

public interface IMapper
{
    object MapToEntity<TModel>(TModel model);
    TModel MapToModel<TModel>(object entity);
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
            {
                value = nodeMap.FromEnum[value.ToString()];
            }

            if (nodeMap.EntityProperties.TryGetValue(fieldMap.DestinationName, out var destProp))
            {
                destProp.SetValue(entity, value);
            }
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
            {
                value = nodeMap.ToEnum[value.ToString()];
            }

            var destProp = modelType.GetProperty(fieldMap.SourceName);
            if (destProp != null)
            {
                destProp.SetValue(model, value);
            }
        }

        return model;
    }

    private Type ResolveEntityType(NodeMap nodeMap)
    {
        var entityName = nodeMap.FieldMaps
            .Select(f => f.DestinationEntity)
            .FirstOrDefault();

        var fullName = $"Database.Entity.{entityName}";
        var type = Type.GetType(fullName);

        if (type == null)
            throw new InvalidOperationException($"Cannot resolve entity type {fullName}");

        return type;
    }
}