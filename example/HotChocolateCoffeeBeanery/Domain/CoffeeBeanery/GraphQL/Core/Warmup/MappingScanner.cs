using System;
using System.Linq;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Mapping;

public static class MappingScanner
{
    public static void ScanAndRegister(Assembly modelAssembly, Assembly entityAssembly)
    {
        // Look across both assemblies for static methods
        var allTypes = modelAssembly.GetTypes()
            .Concat(entityAssembly.GetTypes());

        foreach (var type in allTypes)
        {
            // We're looking for static methods that return a MappingDefinition
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.ReturnType == typeof(MappingDefinition));

            foreach (var method in methods)
            {
                var returnType = method.ReturnType; // MappingDefinition

                // Invoke the method to get the MappingDefinition
                var mappingDefinition = method.Invoke(null, null) as MappingDefinition;

                if (mappingDefinition == null || mappingDefinition.EntityMaps.Count == 0)
                    continue;

                // Register each EntityMap in the MappingRegistry
                foreach (var entityMap in mappingDefinition.EntityMaps)
                {
                    // Use reflection to get the generic arguments for EntityMap<TModel, TEntity>
                    var entityMapType = entityMap.GetType();

                    // Check if entityMapType is a generic type (EntityMap<TModel, TEntity>)
                    if (entityMapType.IsGenericType && entityMapType.GetGenericTypeDefinition() == typeof(EntityMap<,>))
                    {
                        // Get the model and entity types
                        var genericArguments = entityMapType.GetGenericArguments();
                        var modelType = genericArguments[0];  // TModel
                        var entityType = genericArguments[1]; // TEntity

                        // Register the map in the registry
                        var registerMethod = typeof(MappingRegistry)
                            .GetMethod(nameof(MappingRegistry.Register))!
                            .MakeGenericMethod(modelType, entityType);

                        // Invoke the register method with the EntityMap
                        registerMethod.Invoke(null, new object[] { entityMap });
                    }
                }
            }
        }
    }
}
