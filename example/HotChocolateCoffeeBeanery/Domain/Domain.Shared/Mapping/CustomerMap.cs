// using CoffeeBeanery.GraphQL.Core.Mapping;
// using CoffeeBeanery.GraphQL.Core.Sql;
// using Domain.Model;
// using DataEntity = Database.Entity;
//
// namespace Domain.Shared.Mapping;
//
// public class CustomerMap : IMappingRegistration
// {
//     public void Register()
//     {
//         var customer = new NodeMap
//         {
//             IsModel = true
//         };
//
//         customer.LinkKeys.Add(new LinkKey()
//         {
//             From = nameof(Customer),
//             FromColumn = nameof(Customer.CustomerKey),
//             To = nameof(ContactPoint),
//             ToColumn = nameof(ContactPoint.CustomerKey),
//             RelationshipType = RelationshipType.OneToMany
//         });
//
//         customer.LinkKeys.Add(new LinkKey()
//         {
//             From = nameof(Customer),
//             FromColumn = nameof(Customer.CustomerKey),
//             To = nameof(Product),
//             ToColumn = nameof(Customer.CustomerKey)
//         });
//         
//         MappingRegistry.Register(nameof(Customer), customer);
//     }
//     
//     public string MappingNode { get; set; } = nameof(Customer);
//     
//     public static void MapTo(CustomerCustomerEdge customerCustomerEdge, DataEntity.Customer customer, bool isInnerCustomer)
//     {
//         customerCustomerEdge ??= new CustomerCustomerEdge();
//
//         if (isInnerCustomer)
//         {
//             customerCustomerEdge.InnerCustomerKey = customer.CustomerKey;    
//         }
//         else
//         {
//             customerCustomerEdge.OuterCustomerKey = customer.CustomerKey;
//         }
//     }
//     
//     public static void MapTo(CustomerBankingRelationship customerBankingRelationshipCustomer, Customer customer)
//     {
//         customerBankingRelationshipCustomer ??= new CustomerBankingRelationship();
//         customerBankingRelationshipCustomer.CustomerKey = customer.CustomerKey;
//     }
//     
//     public static void MapTo(DataEntity.CustomerBankingRelationship customerBankingRelationshipCustomer, Customer customer)
//     {
//         customerBankingRelationshipCustomer ??= new DataEntity.CustomerBankingRelationship();
//         customerBankingRelationshipCustomer.CustomerKey = customer.CustomerKey;
//     }
// }