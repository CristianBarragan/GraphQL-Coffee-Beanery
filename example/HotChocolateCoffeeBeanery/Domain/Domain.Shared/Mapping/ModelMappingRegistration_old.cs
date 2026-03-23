// using CoffeeBeanery.GraphQL.Core.Mapping;
// using CoffeeBeanery.GraphQL.Core.Sql;
// using Domain.Model;
// using DataEntity = Database.Entity;
// using DataGraph = Database.Graph;
// using System;
// using System.Collections.Generic;
//
// namespace Domain.Shared.Mapping
// {
//     public class ModelMappingRegistration : IMappingRegistration
//     {
//         public Dictionary<string, NodeMap> Register()
//         {
//             var mappings = new Dictionary<string, NodeMap>();
//             
//             //-----------------------------------------
//             // CUSTOMER CUSTOMER RELATIONSHIP EDGE
//             //-----------------------------------------
//             var customerCustomerRelationshipEdge = new NodeMap
//             {
//                 Id = 1,
//                 Schema = nameof(DataGraph.CustomerCustomerRelationshipEdge),
//                 IsGraph = true,
//                 IsModel = true,
//                 IsEntity = true
//             };
//             
//             // customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
//             // {
//             //     SourceName = nameof(CustomerCustomerEdge.Clause),
//             //     DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship)
//             //     // ,
//             //     // DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id)
//             // });
//             //
//             // customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
//             // {
//             //     SourceName = nameof(CustomerCustomerEdge.LevelDepth),
//             //     DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship)
//             //     // ,
//             //     // DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id)
//             // });
//             //
//             // customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
//             // {
//             //     SourceName = nameof(CustomerCustomerEdge.LevelDirection),
//             //     DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship)
//             //     // ,
//             //     // DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id)
//             // });
//             
//             customerCustomerRelationshipEdge.Children.Add(nameof(DataEntity.CustomerCustomerRelationship));
//             customerCustomerRelationshipEdge.Children.Add(nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer));
//             customerCustomerRelationshipEdge.Children.Add(nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer));
//             
//             customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(DataGraph.CustomerCustomerRelationshipEdge.Id),
//                 DestinationEntity = nameof(DataGraph.CustomerCustomerRelationshipEdge),
//                 DestinationName = nameof(DataGraph.CustomerCustomerRelationshipEdge.Id)
//             });
//             
//             customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(CustomerCustomerEdge.InnerCustomerKey),
//                 DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
//                 DestinationName = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey)
//             });
//             
//             customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(CustomerCustomerEdge.OuterCustomerKey),
//                 DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
//                 DestinationName = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey)
//             });
//             
//             customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey),
//                 DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
//                 DestinationName = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)
//             });
//
//             GetMapping(mappings,
//                 MappingRegistry.Register(typeof(CustomerCustomerEdge), typeof(DataGraph.CustomerCustomerRelationshipEdge),
//                     customerCustomerRelationshipEdge));
//
//             //-----------------------------------------
//             // CUSTOMER
//             //-----------------------------------------
//             var cust = new NodeMap
//             {
//                 Id = 3,
//                 Schema = nameof(DataEntity.Schema.Banking)
//             };
//
//             cust.IsEntity = true;
//             cust.IsModel = true;
//
//             cust.Children.Add(nameof(DataEntity.ContactPoint));
//             cust.Children.Add(nameof(DataEntity.CustomerBankingRelationship));
//
//             cust.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.Customer), nameof(DataEntity.Customer.CustomerKey)));
//
//             cust.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(DataEntity.Customer.Id),
//                 DestinationEntity = nameof(DataEntity.Customer),
//                 DestinationName = nameof(DataEntity.Customer.Id)
//             });
//
//             cust.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Customer.CustomerKey),
//                 DestinationEntity = nameof(DataEntity.Customer),
//                 DestinationName = nameof(DataEntity.Customer.CustomerKey)
//             });
//
//             cust.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Customer.FirstNaming),
//                 DestinationEntity = nameof(DataEntity.Customer),
//                 DestinationName = nameof(DataEntity.Customer.FirstName)
//             });
//
//             cust.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Customer.LastNaming),
//                 DestinationEntity = nameof(DataEntity.Customer),
//                 DestinationName = nameof(DataEntity.Customer.LastName)
//             });
//
//             cust.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Customer.FullNaming),
//                 DestinationEntity = nameof(DataEntity.Customer),
//                 DestinationName = nameof(DataEntity.Customer.FullName)
//             });
//
//             // Enum mapping for CustomerType
//             var custEnums = EnumMapFactory.Create(
//                 new Dictionary<string, (CustomerType, DataEntity.CustomerType)>
//                 {
//                     { $"{nameof(Customer)}~{nameof(DataEntity.Customer)}~{DataEntity.CustomerType.Person}", (CustomerType.Person, DataEntity.CustomerType.Person) },
//                     { $"{nameof(Customer)}~{nameof(DataEntity.Customer)}~{DataEntity.CustomerType.Organisation}", (CustomerType.Organisation, DataEntity.CustomerType.Organisation) }
//                 },
//                 new Dictionary<string, (DataEntity.CustomerType, CustomerType)>
//                 {
//                     { $"{nameof(DataEntity.Customer)}~{nameof(Customer)}~{CustomerType.Person}", (DataEntity.CustomerType.Person, CustomerType.Person) },
//                     { $"{nameof(DataEntity.Customer)}~{nameof(Customer)}~{CustomerType.Organisation}", (DataEntity.CustomerType.Organisation, CustomerType.Organisation) }
//                 });
//
//             cust.FromEnum = custEnums.from;
//             cust.ToEnum = custEnums.to;
//
//             // GetMapping(mappings, MappingRegistry.Register(typeof(Customer), typeof(DataEntity.Customer), cust), nameof(Customer));
//             GetMapping(mappings, MappingRegistry.Register(typeof(Customer), typeof(DataEntity.Customer), cust, 
//                 nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer)), nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer));
//             GetMapping(mappings, MappingRegistry.Register(typeof(Customer), typeof(DataEntity.Customer), cust, 
//                 nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer)), nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer));
//
//             //-----------------------------------------
//             // CONTACTPOINT
//             //-----------------------------------------
//             
//
//             //-----------------------------------------
//             // CONTRACT
//             //-----------------------------------------
//             
//
//             //-----------------------------------------
//             // ACCOUNT
//             //-----------------------------------------
//             
//
//             //-----------------------------------------
//             // CUSTOMERBANKINGRELATIONSHIP
//             //-----------------------------------------
//             
//
//             //-----------------------------------------
//             // CUSTOMERCUSTOMERRELATIONSHIP
//             //-----------------------------------------
//             
//
//             //-----------------------------------------
//             // TRANSACTION
//             //-----------------------------------------
//             
//
//             //-----------------------------------------
//             // PRODUCT
//             //-----------------------------------------
//             
//             
//             return mappings;
//         }
//
//         private void GetMapping(Dictionary<string, NodeMap> mappings, NodeMap nodeMap, string alias = "")
//         {
//             if (string.IsNullOrEmpty(alias))
//             {
//                 alias = nodeMap.ModelType.Name;
//             }
//             
//             if (nodeMap.ModelType != null)
//             {
//                 mappings.TryAdd(alias, nodeMap);
//             }
//             
//             if (nodeMap.EntityType != null)
//             {
//                 mappings.TryAdd(alias, nodeMap);    
//             }
//         }
//     }
// }