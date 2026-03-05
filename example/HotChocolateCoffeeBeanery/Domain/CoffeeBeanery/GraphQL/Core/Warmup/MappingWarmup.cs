using System.Linq.Expressions;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Helper;
using CoffeeBeanery.GraphQL.Core.Mapping;

namespace CoffeeBeanery.GraphQL.Core.Warmup;

public static class MappingWarmup
{
    public static void Warmup(IReadOnlyDictionary<string, NodeMap> mapping)
    {
        foreach (var map in mapping.Values)
        {
            if (map.ModelType == null || map.EntityType == null)
            {
                continue;
            }
            
            WarmupMap(map);
            BulkMapper.Compile(map);
        }
    }

    private static void WarmupMap(NodeMap map)
    {
        foreach (var field in map.FieldMaps.Where(a => a.DestinationName != "Id"))
        {
            var modelProp = map.ModelType.GetProperty(field.SourceName);
            var entityProp = map.EntityType.GetProperty(field.DestinationName);

            if (modelProp != null)
                map.ModelProperties[field.SourceName] = modelProp;

            if (entityProp != null)
                map.EntityProperties[field.DestinationName] = entityProp;
        }
    }

    // private static void CompileMapper(NodeMap map)
    // {
    //     var method = typeof(MappingWarmup)
    //         .GetMethod(nameof(CompileGeneric), BindingFlags.NonPublic | BindingFlags.Static)
    //         .MakeGenericMethod(map.ModelType, map.EntityType);
    //
    //     method.Invoke(null, new object[] { map });
    // }
    //
    // private static void CompileGeneric<TModel, TEntity>(NodeMap map)
    // {
    //     var mappings = map.FieldMaps.Where(f => f.SourceName != "Id").Select(f =>
    //     {
    //         var srcParam = Expression.Parameter(typeof(TModel), "x");
    //         var srcProp = Expression.Property(srcParam, f.SourceName);
    //         var srcBody = Expression.Convert(srcProp, typeof(object));  
    //         var dstParam = Expression.Parameter(typeof(TEntity), "x");
    //         var dstProp = Expression.Property(dstParam, f.DestinationName);
    //         var dstBody = Expression.Convert(dstProp, typeof(object));
    //
    //         return new PropertyMapping<TModel, TEntity>
    //         {
    //             SourceExpression =
    //                 Expression.Lambda<Func<TModel, object>>(srcBody, srcParam),
    //
    //             DestinationExpression =
    //                 Expression.Lambda<Func<TEntity, object>>(dstBody, dstParam),
    //
    //             FromEnum = map.FromEnum,
    //             ToEnum = map.ToEnum
    //         };
    //     }).ToArray();
    //
    //     var compiled = BulkMapper.Compile<TModel, TEntity>(mappings);
    //
    //     map.MapToEntityCompiled = (src, dst) =>
    //         compiled((TModel)src, (TEntity)dst);
    // }
}