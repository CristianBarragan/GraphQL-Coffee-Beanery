using System;
using System.Linq;
using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Configuration;
using CoffeeBeanery.GraphQL.Core.Helper;
using CoffeeBeanery.GraphQL.Core.Registry;

public static class MappingScanner
{
    public static void ScanAndRegister(Assembly modelAssembly, Assembly entityAssembly)
    {
        // Look across both assemblies for mapping methods
        var allTypes = modelAssembly.GetTypes()
            .Concat(entityAssembly.GetTypes());

        foreach (var type in allTypes)
        {
            // We’re looking for static methods returning PropertyMapping<T,U>[]
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                .Where(m => m.ReturnType.IsArray &&
                            m.ReturnType.GetElementType()?.IsGenericType == true &&
                            m.ReturnType.GetElementType()!
                                .GetGenericTypeDefinition() == typeof(PropertyMapping<,>));

            foreach (var method in methods)
            {
                var returnType = method.ReturnType;       // e.g. PropertyMapping<A,B>[]
                var genericArgs = returnType.GetElementType()!.GetGenericArguments();
                var modelType  = genericArgs[0];
                var entityType = genericArgs[1];

                // Invoke the method to get the mapping array
                var rawMappings = method.Invoke(null, null) as Array ?? Array.Empty<object>();

                if (rawMappings.Length == 0)
                    continue;

                // Now call MappingRegistry.Register<TModel,TEntity>( PropertyMapping<TModel,TEntity>[] )
                var registerMethod = typeof(MappingRegistry)
                    .GetMethod(nameof(MappingRegistry.Register))!
                    .MakeGenericMethod(modelType, entityType);

                // Cast the raw array to the correct typed array
                var typedMappings = Array.CreateInstance(
                    typeof(PropertyMapping<,>).MakeGenericType(modelType, entityType),
                    rawMappings.Length);

                for (int i = 0; i < rawMappings.Length; i++)
                {
                    typedMappings.SetValue(rawMappings.GetValue(i), i);
                }

                registerMethod.Invoke(null, new object[] { typedMappings });
            }
        }
    }
}
