// using CoffeeBeanery.GraphQL.Core.Mapping;
// using CoffeeBeanery.GraphQL.Core.Sql;
// using Domain.Model;
// using DataEntity = Database.Entity;
// using DataGraph  = Database.Graph;
//
// namespace Domain.Shared.Mapping;
//
// public class CustomerCustomerRelationshipEdgeMapping 
//     : BaseMappingRegistration<CustomerCustomerRelationshipEdge, DataGraph.CustomerCustomerRelationshipEdge>
// {
//     protected override string Alias => nameof(CustomerCustomerRelationshipEdge);
//
//     protected override NodeMap BuildMap()
//     {
//         var map = new NodeMap
//         {
//             Schema  = nameof(DataGraph.CustomerCustomerRelationshipEdge),
//             IsGraph = true,
//             IsModel = true
//         };
//
//         map.ModelChildren.AddRange(new[]
//         {
//             new LinkKey
//             {
//                 From       = nameof(CustomerCustomerRelationshipEdge),
//                 FromColumn = nameof(CustomerCustomerRelationshipEdge.CustomerCustomerRelationshipKey),
//                 To         = nameof(CustomerCustomerEdge),
//                 ToColumn   = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey)
//             },
//             new LinkKey
//             {
//                 From       = nameof(CustomerCustomerRelationshipEdge),
//                 FromColumn = nameof(CustomerCustomerRelationshipEdge.OuterCustomerKey),
//                 To         = nameof(CustomerCustomerEdge),
//                 ToColumn   = nameof(CustomerCustomerEdge.OuterCustomer)
//             },
//             new LinkKey
//             {
//                 From       = nameof(CustomerCustomerRelationshipEdge),
//                 FromColumn = nameof(CustomerCustomerRelationshipEdge.InnerCustomerKey),
//                 To         = nameof(CustomerCustomerEdge),
//                 ToColumn   = nameof(CustomerCustomerEdge.InnerCustomer)
//             }
//         });
//
//         map.ModelToEntityLinks.AddRange(new[]
//         {
//             new LinkKey
//             {
//                 From       = nameof(CustomerCustomerRelationshipEdge),
//                 FromColumn = nameof(CustomerCustomerRelationshipEdge.CustomerCustomerRelationshipKey),
//                 To         = nameof(DataEntity.CustomerCustomerRelationship),
//                 ToColumn   = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)
//             },
//             new LinkKey
//             {
//                 From       = nameof(CustomerCustomerRelationshipEdge),
//                 FromColumn = nameof(CustomerCustomerRelationshipEdge.InnerCustomerKey),
//                 To         = nameof(DataEntity.Customer),
//                 ToColumn   = nameof(DataEntity.Customer.CustomerKey)
//             },
//             new LinkKey
//             {
//                 From       = nameof(CustomerCustomerRelationshipEdge),
//                 FromColumn = nameof(CustomerCustomerRelationshipEdge.OuterCustomerKey),
//                 To         = nameof(DataEntity.Customer),
//                 ToColumn   = nameof(DataEntity.Customer.CustomerKey)
//             }
//         });
//
//         map.EntityChildren.AddRange(new[]
//         {
//             new LinkKey
//             {
//                 From       = nameof(DataGraph.CustomerCustomerRelationshipEdge),
//                 FromColumn = nameof(DataGraph.CustomerCustomerRelationshipEdge.InnerCustomerId),
//                 To         = nameof(DataEntity.Customer),
//                 ToColumn   = nameof(DataEntity.Customer.Id)
//             },
//             new LinkKey
//             {
//                 From       = nameof(DataGraph.CustomerCustomerRelationshipEdge),
//                 FromColumn = nameof(DataGraph.CustomerCustomerRelationshipEdge.OuterCustomerId),
//                 To         = nameof(DataEntity.Customer),
//                 ToColumn   = nameof(DataEntity.Customer.Id)
//             },
//             new LinkKey
//             {
//                 From       = nameof(DataGraph.CustomerCustomerRelationshipEdge),
//                 FromColumn = nameof(DataGraph.CustomerCustomerRelationshipEdge.Id),
//                 To         = nameof(DataEntity.CustomerCustomerRelationship),
//                 ToColumn   = nameof(DataEntity.CustomerCustomerRelationship.Id)
//             }
//         });
//
//         map.FieldMaps.AddRange(new[]
//         {
//             new FieldMap
//             {
//                 SourceName        = nameof(DataGraph.CustomerCustomerRelationshipEdge.Id),
//                 DestinationEntity = nameof(DataGraph.CustomerCustomerRelationshipEdge),
//                 DestinationName   = nameof(DataGraph.CustomerCustomerRelationshipEdge.Id)
//             },
//             new FieldMap
//             {
//                 SourceName        = nameof(DataGraph.CustomerCustomerRelationshipEdge.InnerCustomerId),
//                 DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
//                 DestinationName   = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerId)
//             },
//             new FieldMap
//             {
//                 SourceName        = nameof(DataGraph.CustomerCustomerRelationshipEdge.OuterCustomerId),
//                 DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
//                 DestinationName   = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerId)
//             },
//             new FieldMap
//             {
//                 SourceName        = nameof(CustomerCustomerRelationshipEdge.OuterCustomerKey),
//                 DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
//                 DestinationName   = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey)
//             },
//             new FieldMap
//             {
//                 SourceName        = nameof(CustomerCustomerRelationshipEdge.OuterCustomerKey),
//                 DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
//                 DestinationName   = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey)
//             }
//         });
//
//         return map;
//     }
// }