// using CoffeeBeanery.GraphQL.Core.Mapping;
// using CoffeeBeanery.GraphQL.Core.Sql;
// using Database.Graph;
// using Domain.Model;
// using Domain.Shared.Mapping;
// using CustomerCustomerEdge = Database.Graph.CustomerCustomerEdge;
// using DataEntity = Database.Entity;
//
// public class CustomerCustomerRelationshipMappingSet : IMappingSet<CustomerMappingType, Domain.Model.Model>
// {
//     public void Register(CustomerMappingType type, Domain.Model.Model model)
//     {
//         new CustomerCustomerRelationshipMapping("", model.ToString()).Register();
//     }
// }
//
// public partial class CustomerCustomerRelationshipMapping
//     : BaseMappingRegistration<Domain.Model.CustomerCustomerEdge, DataEntity.CustomerCustomerRelationship>
// {
//     public CustomerCustomerRelationshipMapping(string alias, string model) : base(alias, model) { }
//
//     protected override NodeMap BuildMap()
//     {
//         // FIX: ModelName must be set explicitly here, same as CustomerCustomerEdgeMapping
//         // does. Previously this was left unset and BaseMappingRegistration.Register()'s
//         // fallback kicked in:
//         //     if (string.IsNullOrEmpty(map.ModelName))
//         //         map.ModelName = string.IsNullOrEmpty(Prefix) ? Model : Prefix;
//         // `Model` here is `model.ToString()` from the constructor call
//         // `new CustomerCustomerRelationshipMapping("", model.ToString())`, where `model` is
//         // a Domain.Model.Model enum value that can stringify to "CustomerCustomerRelationship"
//         // (the backing entity's name) - NOT this mapping's actual model name,
//         // "CustomerCustomerEdge". That mismatch was silently breaking any downstream lookup
//         // that compares ModelName against the model type (e.g. ProcessService's
//         // entityTree.ModelName.Matches(rootTree.ModelName)).
//         var map = new NodeMap
//         {
//             ModelName = nameof(Domain.Model.CustomerCustomerEdge),
//             Schema    = nameof(DataEntity.Schema.Banking)
//         };
//         
//         
//
//         // map.EntityChildren.Add(
//         //     new EntityKey {
//         //         AliasFrom  = A(nameof(DataEntity.CustomerCustomerRelationship)),
//         //         From       = nameof(DataEntity.CustomerCustomerRelationship),
//         //         FromColumn = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerId),
//         //         AliasTo    = A(nameof(CustomerMappingType.InnerCustomer), nameof(DataEntity.Customer)),
//         //         To         = nameof(CustomerMappingType.InnerCustomer),
//         //         ToColumn   = nameof(DataEntity.Customer.Id)
//         // });
//         //
//         // map.EntityChildren.Add(
//         //     new EntityKey {
//         //         AliasFrom  = A(nameof(DataEntity.CustomerCustomerRelationship)),
//         //         From       = nameof(DataEntity.CustomerCustomerRelationship),
//         //         FromColumn = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerId),
//         //         AliasTo    = A(nameof(CustomerMappingType.OuterCustomer), nameof(DataEntity.Customer)),
//         //         To         = nameof(CustomerMappingType.OuterCustomer),
//         //         ToColumn   = nameof(DataEntity.Customer.Id)
//         //     });
//
//         map.UpsertKeys.Add(new UpsertKey(
//             nameof(DataEntity.CustomerCustomerRelationship),
//             nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)));
//
//         // only the enum needs an explicit FieldMap - everything else is name-convention
//         map.FieldMaps.Add(new FieldMap
//         {
//             SourceName        = nameof(Domain.Model.CustomerCustomerEdge.CustomerCustomerRelationshipType),
//             DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
//             DestinationName   = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipType),
//             FromEnum = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
//             {
//                 { CustomerCustomerRelationshipType.Divorced.ToString(), (int)CustomerCustomerRelationshipType.Divorced },
//                 { CustomerCustomerRelationshipType.Family.ToString(),   (int)CustomerCustomerRelationshipType.Family },
//                 { CustomerCustomerRelationshipType.Partner.ToString(),  (int)CustomerCustomerRelationshipType.Partner },
//                 { CustomerCustomerRelationshipType.Single.ToString(),   (int)CustomerCustomerRelationshipType.Single },
//                 { CustomerCustomerRelationshipType.Widow.ToString(),    (int)CustomerCustomerRelationshipType.Widow }
//             },
//             ToEnum = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
//             {
//                 { DataEntity.CustomerCustomerRelationshipType.Divorced.ToString(), (int)DataEntity.CustomerCustomerRelationshipType.Divorced },
//                 { DataEntity.CustomerCustomerRelationshipType.Family.ToString(),   (int)DataEntity.CustomerCustomerRelationshipType.Family },
//                 { DataEntity.CustomerCustomerRelationshipType.Partner.ToString(),  (int)DataEntity.CustomerCustomerRelationshipType.Partner },
//                 { DataEntity.CustomerCustomerRelationshipType.Single.ToString(),   (int)DataEntity.CustomerCustomerRelationshipType.Single },
//                 { DataEntity.CustomerCustomerRelationshipType.Widow.ToString(),    (int)DataEntity.CustomerCustomerRelationshipType.Widow }
//             }
//         });
//
//         map.GraphMap = new GraphMap
//         {
//             GraphName     = G(nameof(CustomerCustomerEdge)),
//             EdgeLabel     = nameof(CustomerCustomerEdge),
//             EdgeKeyColumn = nameof(Domain.Model.CustomerCustomerEdge.CustomerCustomerRelationshipKey),
//             FromVertex = new GraphVertex { Label = nameof(Customer), KeyColumn = nameof(Domain.Model.CustomerCustomerEdge.InnerCustomerKey), AliasTo = A(nameof(Customer)) },
//             ToVertex   = new GraphVertex { Label = nameof(Customer), KeyColumn = nameof(Domain.Model.CustomerCustomerEdge.OuterCustomerKey), AliasTo = A(nameof(Customer)) },
//             FromJoinColumn = nameof(Customer.CustomerKey),
//             ToJoinColumn   = nameof(Customer.CustomerKey)
//         };
//
//         return map;
//     }
// }