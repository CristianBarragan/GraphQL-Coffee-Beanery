// using System.Reflection;
// using CoffeeBeanery.CQRS;
// using CoffeeBeanery.GraphQL.Core.Sql;
// using CoffeeBeanery.Service;
// using Domain.Model;
// using Npgsql;
//
// namespace Domain.Shared.Query;
//
// public class CustomerCustomerEdgeQueryHandler<M> : ProcessQuery<M>, IQuery<ProcessQueryParameters,
//     (List<M> list, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>
//     where M : class, new()
// {
//     private readonly IMapper _mapper;
//
//     // Cache edge properties once — avoids repeated GetProperties() calls per row
//     private static readonly Type EdgeType = typeof(CustomerCustomerEdge);
//     private static readonly PropertyInfo[] EdgeProperties =
//         EdgeType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
//
//     public CustomerCustomerEdgeQueryHandler(
//         ILoggerFactory loggerFactory,
//         NpgsqlConnection dbConnection,
//         IMapper mapper) : base(loggerFactory, dbConnection)
//     {
//         _mapper = mapper;
//     }
//
//     public override (List<M> models, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)
//         MappingConfiguration(List<M> models, SqlStructure sqlStructure, object[] map, List<Type> types, List<string> aliases)
//     {
//         var edges = new Dictionary<Guid, CustomerCustomerEdge>();
//         var totalCount  = 0;
//         var pageRecords = 0;
//
//         for (int i = 0; i < map.Length; i++)
//         {
//             var item  = map[i];
//             var alias = aliases[i];
//
//             if (item == null)
//             {
//                 Console.WriteLine($"[WARN] Null map at index {i} with alias {alias}");
//                 continue;
//             }
//
//             // Handle meta types early
//             if (item is TotalPageRecords tpr)
//             {
//                 pageRecords = tpr.PageRecords;
//                 continue;
//             }
//
//             if (item is TotalRecordCount trc)
//             {
//                 totalCount = trc.RecordCount;
//                 continue;
//             }
//
//             var mapped = _mapper.MapByAlias(item.GetType(), item, alias);
//             if (mapped == null) continue;
//
//             var mappedType = mapped.GetType();
//
//             // 🔑 STEP 1: Extract relationship key (critical)
//             Guid? relationshipKey = null;
//
//             // Try direct match first
//             var keyProp = mappedType.GetProperty("CustomerCustomerRelationshipKey");
//             if (keyProp != null)
//             {
//                 relationshipKey = keyProp.GetValue(mapped) as Guid?;
//             }
//
//             // If still null, try to get it from edge later (fallback)
//             if (!relationshipKey.HasValue)
//             {
//                 // You could enhance this if key lives elsewhere
//                 continue;
//             }
//
//             // 🔑 STEP 2: Get or create edge
//             if (!edges.TryGetValue(relationshipKey.Value, out var edge))
//             {
//                 edge = new CustomerCustomerEdge();
//                 edges[relationshipKey.Value] = edge;
//             }
//
//             var edgeType = typeof(CustomerCustomerEdge);
//
//             // 🔗 STEP 3: Try to map as nested object using alias
//             var targetProp = edgeType.GetProperties()
//                 .FirstOrDefault(p => 
//                     string.Equals(p.Name, alias, StringComparison.OrdinalIgnoreCase)
//                     && p.PropertyType.IsAssignableFrom(mappedType));
//
//             if (targetProp != null)
//             {
//                 targetProp.SetValue(edge, mapped);
//
//                 // 📌 Bubble scalar values (like keys)
//                 foreach (var sourceProp in mappedType.GetProperties(BindingFlags.Public | BindingFlags.Instance))
//                 {
//                     if (!sourceProp.CanRead || !IsScalarOrNullable(sourceProp.PropertyType))
//                         continue;
//
//                     var edgeProp = edgeType.GetProperty(sourceProp.Name,
//                         BindingFlags.Public | BindingFlags.Instance);
//
//                     if (edgeProp == null || !edgeProp.CanWrite)
//                         continue;
//
//                     if (!IsScalarOrNullable(edgeProp.PropertyType))
//                         continue;
//
//                     var value = sourceProp.GetValue(mapped);
//                     if (value != null)
//                         edgeProp.SetValue(edge, value);
//                 }
//             }
//             else
//             {
//                 // 🧱 STEP 4: Flat fallback mapping
//                 foreach (var edgeProp in edgeType.GetProperties()
//                              .Where(p => p.CanWrite && IsScalarOrNullable(p.PropertyType)))
//                 {
//                     var sourceProp = mappedType.GetProperty(edgeProp.Name,
//                         BindingFlags.Public | BindingFlags.Instance);
//
//                     if (sourceProp == null) continue;
//
//                     var value = sourceProp.GetValue(mapped);
//                     if (value != null)
//                         edgeProp.SetValue(edge, value);
//                 }
//             }
//         }
//
//         // ✅ Final result
//         dynamic list = edges.Values.ToList();
//
//         return (list,
//             sqlStructure.Pagination?.StartCursor,
//             sqlStructure.Pagination?.EndCursor,
//             totalCount,
//             pageRecords);
//         
//         
//         // for (int i = 0; i < map.Length; i++)
//         // {
//         //     Console.WriteLine($"[DEBUG map] i={i} type={map[i]?.GetType().Name} alias={aliases[i]}");
//         // }
//         //
//         // // O(1) lookup by InnerCustomerKey instead of O(n) FindIndex scan
//         // var edgeIndex  = models
//         //     .OfType<CustomerCustomerEdge>()
//         //     .Where(e => e.InnerCustomerKey.HasValue)
//         //     .ToDictionary(e => e.InnerCustomerKey!.Value);
//         //
//         // var totalCount  = 0;
//         // var pageRecords = 0;
//         // var edge        = new CustomerCustomerEdge();
//         //
//         // for (int i = 0; i < map.Length; i++)
//         // {
//         //     if (map[i] is null) continue;
//         //
//         //     if (map[i] is TotalPageRecords tpr)
//         //     {
//         //         pageRecords = tpr.PageRecords;
//         //         continue;
//         //     }
//         //
//         //     if (map[i] is TotalRecordCount trc)
//         //     {
//         //         totalCount = trc.RecordCount;
//         //         continue;
//         //     }
//         //
//         //     var mapped = _mapper.MapByAlias(map[i].GetType(), map[i], aliases[i]);
//         //     if (mapped == null) continue;
//         //
//         //     var mappedType = mapped.GetType();
//         //
//         //     // 1. Try alias as direct property name on the edge (e.g. "InnerCustomer", "OuterCustomer",
//         //     //    "CustomerCustomerRelationship")
//         //     var targetProp = EdgeType.GetProperty(aliases[i]);
//         //
//         //     if (targetProp != null && targetProp.PropertyType.IsAssignableFrom(mappedType))
//         //     {
//         //         // Set the nested object
//         //         targetProp.SetValue(edge, mapped);
//         //
//         //         // Bubble up any scalar/key fields that also live directly on the edge
//         //         // e.g. CustomerCustomerRelationship.CustomerCustomerRelationshipKey
//         //         //   → CustomerCustomerEdge.CustomerCustomerRelationshipKey
//         //         foreach (var sourceProp in mapped.GetType()
//         //             .GetProperties(BindingFlags.Public | BindingFlags.Instance)
//         //             .Where(p => p.CanRead && IsScalarOrNullable(p.PropertyType)))
//         //         {
//         //             var edgeProp = EdgeType.GetProperty(sourceProp.Name,
//         //                 BindingFlags.Public | BindingFlags.Instance);
//         //
//         //             if (edgeProp == null || !edgeProp.CanWrite) continue;
//         //             if (!IsScalarOrNullable(edgeProp.PropertyType)) continue;
//         //
//         //             var value = sourceProp.GetValue(mapped);
//         //             if (value != null)
//         //                 edgeProp.SetValue(edge, value);
//         //         }
//         //     }
//         //     else
//         //     {
//         //         // 2. No matching nested property — copy all writable scalar fields by name
//         //         //    onto the edge directly (flat fallback)
//         //         foreach (var edgeProp in EdgeProperties.Where(p => p.CanWrite && IsScalarOrNullable(p.PropertyType)))
//         //         {
//         //             var sourceProp = mappedType.GetProperty(edgeProp.Name,
//         //                 BindingFlags.Public | BindingFlags.Instance);
//         //
//         //             if (sourceProp == null) continue;
//         //
//         //             var value = sourceProp.GetValue(mapped);
//         //             if (value != null)
//         //                 edgeProp.SetValue(edge, value);
//         //         }
//         //     }
//         // }
//         //
//         // // O(1) upsert into dictionary
//         // if (edge.InnerCustomerKey.HasValue)
//         //     edgeIndex[edge.InnerCustomerKey.Value] = edge;
//         //
//         // // Convert back to List<M>
//         // dynamic list = edgeIndex.Values.ToList();
//         //
//         // return (list,
//         //     sqlStructure.Pagination?.StartCursor,
//         //     sqlStructure.Pagination?.EndCursor,
//         //     totalCount,
//         //     pageRecords);
//     }
//
//     private static bool IsScalarOrNullable(Type t) =>
//         t.IsPrimitive
//         || t == typeof(string)
//         || t == typeof(Guid)
//         || t == typeof(Guid?)
//         || t == typeof(decimal)
//         || t == typeof(decimal?)
//         || t == typeof(DateTime)
//         || t == typeof(DateTime?)
//         || t == typeof(DateTimeOffset)
//         || t == typeof(DateTimeOffset?)
//         || (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Nullable<>));
// }