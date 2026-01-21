// using CoffeeBeanery.GraphQL.Configuration;
// using CoffeeBeanery.GraphQL.Core.Helper;
// using Domain.Model;
// using DataEntity = Database.Entity;
//
// namespace Domain.Shared.Mapping
// {
//     public static class Mappings
//     {
//         public static PropertyMapping<Customer, DataEntity.Customer>[] ModelToEntity()
//             => new Map
//             {
//                 EntityUpsertKeys = new[]
//                 {
//                     new UpsertKey((entity, column) =>
//                     {
//                         entity = nameof(DataEntity.Customer);
//                         column = nameof(DataEntity.Customer.CustomerKey);
//                     })
//                 },
//                 JoinKeys = new[]
//                 {
//                     new JoinKey((fromKey, fromKeyId, ToKey, ToKeyId) =>
//                     {
//                         fromKey = nameof(Customer.CustomerKey);
//                         fromKeyId = nameof(Customer.CustomerKey);
//                         ToKey = nameof(DataEntity.Customer.CustomerKey);
//                         ToKeyId = nameof(DataEntity.Customer.Id);
//                     })
//                 },
//                 Mappings = new[]
//                 {
//                     new PropertyMapping<Customer, DataEntity.Customer>(
//                         s => s.CustomerKey,
//                         d => d.CustomerKey),
//         
//                     new PropertyMapping<Customer, DataEntity.Customer>(
//                         s => s.FirstNaming,
//                         d => d.FirstName),
//         
//                     new PropertyMapping<Customer, DataEntity.Customer>(
//                         s => s.LastNaming,
//                         d => d.LastName),
//         
//                     new PropertyMapping<Customer, DataEntity.Customer>(
//                         s => s.FullNaming,
//                         d => d.FullName
//                         ),
//         
//                     new PropertyMapping<Customer, DataEntity.Customer>(
//                         s => s.CustomerType,
//                         d => d.CustomerType,
//                         fromEnum: new Dictionary<Enum, Enum>
//                         {
//                             { CustomerType.Person, DataEntity.CustomerType.Person },
//                             { CustomerType.Organisation, DataEntity.CustomerType.Organisation }
//                         },
//                         toEnum: new Dictionary<Enum, Enum>
//                         {
//                             { DataEntity.CustomerType.Person, CustomerType.Person },
//                             { DataEntity.CustomerType.Organisation, CustomerType.Organisation }
//                         },
//                         mappedTo: new List<>()
//                         {
//                             PropertyMapping<DataEntity.Customer, DataEntity.CustomerBankingRelationship>().EntityToEntity();
//                             PropertyMapping<DataEntity.Customer, DataEntity.ContactPoint>().EntityToEntity();
//                         }
//                 }
//             };
//         
//         public static PropertyMapping<DataEntity.Customer, Customer>[] EntityToModel()
//             => new[]
//             {
//                 new PropertyMapping<DataEntity.Customer, Customer>(
//                     s => s.CustomerKey,
//                     d => d.CustomerKey)
//             };
//         
//         public static Customer MapEntityToModel(DataEntity.Customer e)
//         {
//             var c = new Customer();
//             var maps = EntityToModel();
//             var mapper = maps.CompileMap();
//             mapper(e, c);
//             return c;
//         }
//         
//         public static DataEntity.Customer MapModelToEntity(Customer m)
//         {
//             var e = new DataEntity.Customer();
//             var maps = ModelToEntity();
//             var mapper = maps.CompileMap();
//             mapper(m, e);
//             return e;
//         }
//     }
// }
