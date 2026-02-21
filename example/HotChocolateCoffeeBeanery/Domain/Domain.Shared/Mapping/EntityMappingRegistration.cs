// using CoffeeBeanery.GraphQL.Core.Mapping;
// using CoffeeBeanery.GraphQL.Core.Sql;
// using Domain.Model;
// using DataEntity = Database.Entity;
// using System;
// using System.Collections.Generic;
//
// namespace Domain.Shared.Mapping
// {
//     public class EntityMappingRegistration : IMappingRegistration
//     {
//         
//             
//             // cust.FieldMaps.Add(new FieldMap
//             // {
//             //     SourceName = nameof(Customer.CustomerKey),
//             //     DestinationEntity = nameof(DataEntity.Customer),
//             //     DestinationName = nameof(DataEntity.Customer.CustomerKey)
//             // });
//             //
//             // cust.FieldMaps.Add(new FieldMap
//             // {
//             //     SourceName = nameof(Customer.FirstNaming),
//             //     DestinationEntity = nameof(DataEntity.Customer),
//             //     DestinationName = nameof(DataEntity.Customer.FirstName)
//             // });
//             //
//             // cust.FieldMaps.Add(new FieldMap
//             // {
//             //     SourceName = nameof(Customer.LastNaming),
//             //     DestinationEntity = nameof(DataEntity.Customer),
//             //     DestinationName = nameof(DataEntity.Customer.LastName)
//             // });
//             //
//             // cust.FieldMaps.Add(new FieldMap
//             // {
//             //     SourceName = nameof(Customer.FullNaming),
//             //     DestinationEntity = nameof(DataEntity.Customer),
//             //     DestinationName = nameof(DataEntity.Customer.FullName)
//             // });
//             //
//             // // Enum mapping for CustomerType
//             // var custEnums = EnumMapFactory.Create(
//             //     new Dictionary<CustomerType, DataEntity.CustomerType>
//             //     {
//             //         { CustomerType.Person, DataEntity.CustomerType.Person },
//             //         { CustomerType.Organisation, DataEntity.CustomerType.Organisation }
//             //     },
//             //     new Dictionary<DataEntity.CustomerType, CustomerType>
//             //     {
//             //         { DataEntity.CustomerType.Person, CustomerType.Person },
//             //         { DataEntity.CustomerType.Organisation, CustomerType.Organisation }
//             //     });
//             //
//             // cust.FromEnum = custEnums.from;
//             // cust.ToEnum = custEnums.to;
//
//             // MappingRegistry.Register(nameof(Customer), cust);
//
//             //-----------------------------------------
//             // CONTACTPOINT
//             //-----------------------------------------
//             
//
//             cp.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(ContactPoint.ContactPointKey),
//                 DestinationEntity = nameof(DataEntity.ContactPoint),
//                 DestinationName = nameof(DataEntity.ContactPoint.ContactPointKey)
//             });
//
//             cp.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(ContactPoint.ContactPointValue),
//                 DestinationEntity = nameof(DataEntity.ContactPoint),
//                 DestinationName = nameof(DataEntity.ContactPoint.ContactPointValue)
//             });
//
//             cp.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(ContactPoint.CustomerKey),
//                 DestinationEntity = nameof(DataEntity.ContactPoint),
//                 DestinationName = nameof(DataEntity.ContactPoint.CustomerKey)
//             });
//
//             // Enum mapping for ContactPointType
//             var cpEnums = EnumMapFactory.Create(
//                 new Dictionary<ContactPointType, DataEntity.ContactPointType>
//                 {
//                     { ContactPointType.Mobile, DataEntity.ContactPointType.Mobile },
//                     { ContactPointType.Landline, DataEntity.ContactPointType.Landline },
//                     { ContactPointType.Email, DataEntity.ContactPointType.Email }
//                 },
//                 new Dictionary<DataEntity.ContactPointType, ContactPointType>
//                 {
//                     { DataEntity.ContactPointType.Mobile, ContactPointType.Mobile },
//                     { DataEntity.ContactPointType.Landline, ContactPointType.Landline },
//                     { DataEntity.ContactPointType.Email, ContactPointType.Email }
//                 });
//
//             cp.FromEnum = cpEnums.from;
//             cp.ToEnum = cpEnums.to;
//
//             MappingRegistry.Register(nameof(ContactPoint), cp);
//
//             //-----------------------------------------
//             // CONTRACT
//             //-----------------------------------------
//             var contract = new NodeMap
//             {
//                 IsModel = true
//             };
//             
//             cust.LinkKeys.Add(new LinkKey()
//             {
//                 From = nameof(Contract),
//                 FromColumn = nameof(Contract.ContractKey),
//                 To = nameof(Account),
//                 ToColumn = nameof(Account.AccountKey),
//                 RelationshipType = RelationshipType.OneToOne
//             });
//             
//             cust.LinkKeys.Add(new LinkKey()
//             {
//                 From = nameof(Contract),
//                 FromColumn = nameof(Contract.ContractKey),
//                 To = nameof(Transaction),
//                 ToColumn = nameof(Transaction.ContractKey),
//                 RelationshipType = RelationshipType.OneToMany
//             });
//
//             contract.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.Contract),
//                 nameof(DataEntity.Contract.ContractKey)));
//
//             contract.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Contract.ContractKey),
//                 DestinationEntity = nameof(DataEntity.Contract),
//                 DestinationName = nameof(DataEntity.Contract.ContractKey)
//             });
//
//             contract.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Contract.Amount),
//                 DestinationEntity = nameof(DataEntity.Contract),
//                 DestinationName = nameof(DataEntity.Contract.Amount)
//             });
//
//             // Enum mapping for ContractType
//             var contractEnums = EnumMapFactory.Create(
//                 new Dictionary<ContractType, DataEntity.ContractType>
//                 {
//                     { ContractType.CreditCard, DataEntity.ContractType.CreditCard },
//                     { ContractType.Mortgage, DataEntity.ContractType.Mortgage },
//                     { ContractType.PersonalLoan, DataEntity.ContractType.PersonalLoan }
//                 },
//                 new Dictionary<DataEntity.ContractType, ContractType>
//                 {
//                     { DataEntity.ContractType.CreditCard, ContractType.CreditCard },
//                     { DataEntity.ContractType.Mortgage, ContractType.Mortgage },
//                     { DataEntity.ContractType.PersonalLoan, ContractType.PersonalLoan }
//                 });
//
//             contract.FromEnum = contractEnums.from;
//             contract.ToEnum = contractEnums.to;
//
//             MappingRegistry.Register(nameof(Contract), contract);
//
//             //-----------------------------------------
//             // ACCOUNT
//             //-----------------------------------------
//             var acct = new NodeMap
//             {
//                 IsModel = true
//             };
//             
//             acct.LinkKeys.Add(new LinkKey()
//             {
//                 From = nameof(Account),
//                 FromColumn = nameof(Account.AccountKey),
//                 To = nameof(Contract),
//                 ToColumn = nameof(Contract.ContractKey),
//                 RelationshipType = RelationshipType.OneToOne
//             });
//             
//             acct.LinkKeys.Add(new LinkKey()
//             {
//                 From = nameof(Account),
//                 FromColumn = nameof(Account.AccountKey),
//                 To = nameof(Transaction.AccountKey),
//                 RelationshipType = RelationshipType.OneToMany
//             });
//
//             acct.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.Account), nameof(DataEntity.Account.AccountKey)));
//
//             acct.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Account.AccountKey),
//                 DestinationEntity = nameof(DataEntity.Account),
//                 DestinationName = nameof(DataEntity.Account.AccountKey)
//             });
//
//             acct.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Account.AccountNumber),
//                 DestinationEntity = nameof(DataEntity.Account),
//                 DestinationName = nameof(DataEntity.Account.AccountNumber)
//             });
//
//             acct.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Account.AccountName),
//                 DestinationEntity = nameof(DataEntity.Account),
//                 DestinationName = nameof(DataEntity.Account.AccountName)
//             });
//
//             MappingRegistry.Register(nameof(Account), acct);
//
//             //-----------------------------------------
//             // CUSTOMERBANKINGRELATIONSHIP
//             //-----------------------------------------
//             var cbr = new NodeMap
//             {
//                 IsModel = true
//             };
//             
//             acct.LinkKeys.Add(new LinkKey()
//             {
//                 From = nameof(CustomerBankingRelationship),
//                 FromColumn = nameof(CustomerBankingRelationship.CustomerBankingRelationshipKey),
//                 To = nameof(Contract),
//                 ToColumn = nameof(Contract.CustomerBankingRelationshipKey),
//                 RelationshipType = RelationshipType.OneToMany
//             });
//             
//             acct.LinkKeys.Add(new LinkKey()
//             {
//                 From = nameof(Account),
//                 FromColumn = nameof(Account.AccountKey),
//                 To = nameof(Contract),
//                 ToColumn = nameof(Contract.ContractKey),
//                 RelationshipType = RelationshipType.OneToOne
//             });
//
//             cbr.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.CustomerBankingRelationship),
//                 nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey)));
//
//             cbr.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(CustomerBankingRelationship.CustomerBankingRelationshipKey),
//                 DestinationEntity = nameof(DataEntity.CustomerBankingRelationship),
//                 DestinationName = nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey)
//             });
//
//             cbr.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(CustomerBankingRelationship.CustomerKey),
//                 DestinationEntity = nameof(DataEntity.CustomerBankingRelationship),
//                 DestinationName = nameof(DataEntity.CustomerBankingRelationship.CustomerKey)
//             });
//
//             MappingRegistry.Register(nameof(CustomerBankingRelationship), cbr);
//
//             //-----------------------------------------
//             // CUSTOMERCUSTOMERRELATIONSHIP
//             //-----------------------------------------
//             var ccr = new NodeMap
//             {
//                 IsModel = true
//             };
//
//             ccr.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship),
//                 nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)));
//
//             ccr.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipKey),
//                 DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
//                 DestinationName = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)
//             });
//
//             ccr.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipType),
//                 DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
//                 DestinationName = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipType)
//             });
//             
//             MappingRegistry.Register(nameof(CustomerCustomerRelationship), ccr);
//
//             //-----------------------------------------
//             // TRANSACTION
//             //-----------------------------------------
//             var transaction = new NodeMap
//             {
//                 Schema = nameof(DataEntity.Schema.Lending)
//             };
//
//             transaction.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.Transaction),
//                 nameof(DataEntity.Transaction.TransactionKey)));
//
//             transaction.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Transaction.TransactionKey),
//                 DestinationEntity = nameof(DataEntity.Transaction),
//                 DestinationName = nameof(DataEntity.Transaction.TransactionKey)
//             });
//
//             transaction.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Transaction.Amount),
//                 DestinationEntity = nameof(DataEntity.Transaction),
//                 DestinationName = nameof(DataEntity.Transaction.Amount)
//             });
//
//             transaction.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Transaction.Balance),
//                 DestinationEntity = nameof(DataEntity.Transaction),
//                 DestinationName = nameof(DataEntity.Transaction.Balance)
//             });
//
//             MappingRegistry.Register(nameof(Transaction), transaction);
//
//             //-----------------------------------------
//             // PRODUCT
//             //-----------------------------------------
//             var product = new NodeMap
//             {
//                 Schema = nameof(DataEntity.Schema.Banking)
//             };
//             
//             acct.LinkKeys.Add(new LinkKey()
//             {
//                 From = nameof(Product),
//                 FromColumn = nameof(Product.AccountKey),
//                 To = nameof(Account),
//                 ToColumn = nameof(Account.AccountKey),
//                 RelationshipType = RelationshipType.OneToOne
//             });
//             
//             acct.LinkKeys.Add(new LinkKey()
//             {
//                 From = nameof(Product),
//                 FromColumn = nameof(Product.ContractKey),
//                 To = nameof(Contract),
//                 ToColumn = nameof(Contract.ContractKey),
//                 RelationshipType = RelationshipType.OneToOne
//             });
//             
//             acct.LinkKeys.Add(new LinkKey()
//             {
//                 From = nameof(Product),
//                 FromColumn = nameof(Product.CustomerBankingRelationshipKey),
//                 To = nameof(CustomerBankingRelationship),
//                 ToColumn = nameof(CustomerBankingRelationship.CustomerBankingRelationshipKey),
//                 RelationshipType = RelationshipType.OneToOne
//             });
//
//             product.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.CustomerBankingRelationship),
//                 nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey)));
//
//             product.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Product.CustomerBankingRelationshipKey),
//                 DestinationEntity = nameof(DataEntity.CustomerBankingRelationship),
//                 DestinationName = nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey)
//             });
//
//             product.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Product.ContractKey),
//                 DestinationEntity = nameof(DataEntity.Contract),
//                 DestinationName = nameof(DataEntity.Contract.ContractKey)
//             });
//             
//             product.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Product.Amount),
//                 DestinationEntity = nameof(DataEntity.Contract),
//                 DestinationName = nameof(DataEntity.Contract.Amount)
//             });
//
//             product.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Product.CustomerKey),
//                 DestinationEntity = nameof(DataEntity.Customer),
//                 DestinationName = nameof(DataEntity.Customer.CustomerKey)
//             });
//
//             product.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Product.AccountKey),
//                 DestinationEntity = nameof(DataEntity.Account),
//                 DestinationName = nameof(DataEntity.Account.AccountKey)
//             });
//
//             product.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Product.AccountName),
//                 DestinationEntity = nameof(DataEntity.Account),
//                 DestinationName = nameof(DataEntity.Account.AccountName)
//             });
//
//             product.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(Product.AccountNumber),
//                 DestinationEntity = nameof(DataEntity.Account),
//                 DestinationName = nameof(DataEntity.Account.AccountNumber)
//             });
//
//             // Enum mapping for ProductType by value
//             var productEnums = EnumMapFactory.Create(
//                 new Dictionary<ProductType, DataEntity.ContractType>
//                 {
//                     { ProductType.CreditCard, DataEntity.ContractType.CreditCard },
//                     { ProductType.Mortgage, DataEntity.ContractType.Mortgage },
//                     { ProductType.PersonalLoan, DataEntity.ContractType.PersonalLoan }
//                 },
//                 new Dictionary<DataEntity.ContractType, ProductType>
//                 {
//                     { DataEntity.ContractType.CreditCard, ProductType.CreditCard },
//                     { DataEntity.ContractType.Mortgage, ProductType.Mortgage },
//                     { DataEntity.ContractType.PersonalLoan, ProductType.PersonalLoan }
//                 });
//
//             product.FromEnum = productEnums.from;
//             product.ToEnum = productEnums.to;
//
//             MappingRegistry.Register(nameof(Product), product);
//
//             //-----------------------------------------
//             // CUSTOMER CUSTOMER EDGE
//             //-----------------------------------------
//             var customerCustomerEdge = new NodeMap
//             {
//                 Schema = nameof(DataEntity.Schema.Banking),
//                 IsGraph = true
//             };
//             
//             customerCustomerEdge.LinkKeys.Add(new LinkKey()
//             {
//                 From = nameof(CustomerCustomerEdge),
//                 FromColumn = nameof(CustomerCustomerEdge.InnerCustomerKey),
//                 To = nameof(Customer),
//                 ToColumn = nameof(Customer.CustomerCustomerEdgeKey),
//                 RelationshipType = RelationshipType.OneToMany
//             });
//             
//             customerCustomerEdge.LinkKeys.Add(new LinkKey()
//             {
//                 From = nameof(CustomerCustomerEdge),
//                 FromColumn = nameof(CustomerCustomerEdge.OuterCustomerKey),
//                 To = nameof(Customer),
//                 ToColumn = nameof(Customer.CustomerCustomerEdgeKey),
//                 RelationshipType = RelationshipType.OneToMany
//             });
//             
//             customerCustomerEdge.LinkKeys.Add(new LinkKey()
//             {
//                 From = nameof(CustomerCustomerEdge),
//                 FromColumn = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey),
//                 To = nameof(CustomerCustomerRelationship),
//                 ToColumn = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipKey),
//                 RelationshipType = RelationshipType.OneToMany
//             });
//
//             customerCustomerEdge.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship),
//                 nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)));
//             
//             customerCustomerEdge.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey),
//                 DestinationEntity = nameof(CustomerCustomerRelationship),
//                 DestinationName = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipKey)
//             });
//             
//             customerCustomerEdge.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(CustomerCustomerEdge.InnerCustomerKey),
//                 DestinationEntity = nameof(Customer),
//                 DestinationName = nameof(Customer.CustomerKey)
//             });
//             
//             customerCustomerEdge.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(CustomerCustomerEdge.OuterCustomerKey),
//                 DestinationEntity = nameof(CustomerCustomerRelationship),
//                 DestinationName = nameof(Customer.CustomerKey)
//             });
//
//             customerCustomerEdge.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey),
//                 DestinationEntity = nameof(DataEntity.Customer),
//                 DestinationName = nameof(DataEntity.Customer.CustomerKey)
//             });
//             
//             customerCustomerEdge.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(CustomerCustomerEdge.InnerCustomerKey),
//                 DestinationEntity = nameof(DataEntity.Customer),
//                 DestinationName = nameof(DataEntity.Customer.CustomerKey)
//             });
//             
//             customerCustomerEdge.FieldMaps.Add(new FieldMap
//             {
//                 SourceName = nameof(CustomerCustomerEdge.OuterCustomerKey),
//                 DestinationEntity = nameof(DataEntity.Customer),
//                 DestinationName = nameof(DataEntity.Customer.CustomerKey)
//             });
//
//             MappingRegistry.Register(nameof(CustomerCustomerEdge), customerCustomerEdge);
//         }
//     }
// }
