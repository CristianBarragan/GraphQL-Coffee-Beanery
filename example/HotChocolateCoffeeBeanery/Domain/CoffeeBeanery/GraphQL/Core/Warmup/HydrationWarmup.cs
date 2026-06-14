using System.Reflection;
using CoffeeBeanery.GraphQL.Core.Contracts;
 using CoffeeBeanery.GraphQL.Core.Runtime;
 
 namespace CoffeeBeanery.GraphQL.Core.Warmup;
 
 public static class HydrationWarmup
 {
     public static void BuildAll(
         IEnumerable<Type> modelTypes,
         Dictionary<string, List<SelectColumn>> projections,
         RuntimeWarmup warmup)
     {
         foreach (var type in modelTypes)
         {
             if (!projections.TryGetValue(type.Name, out var cols))
                 continue;

             warmup.Hydrators[type] =
                 BuildUntyped(type, cols);
         }
     }

     private static Func<object[], object> BuildUntyped(Type type, List<SelectColumn> cols)
     {
         return (Func<object[], object>)typeof(HydrationWarmup)
             .GetMethod(nameof(BuildGeneric), BindingFlags.NonPublic | BindingFlags.Static)!
             .MakeGenericMethod(type)
             .Invoke(null, new object[] { cols })!;
     }

     private static Func<object[], object> BuildGeneric<T>(List<SelectColumn> cols)
     {
         var typed = HydrationCompiler.Build<T>(cols);
         return row => typed(row);
     }
 }