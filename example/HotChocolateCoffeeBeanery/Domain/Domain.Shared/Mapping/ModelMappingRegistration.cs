using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using Domain.Model;
using DataEntity = Database.Entity;
using DataGraph = Database.Graph;
using System;
using System.Collections.Generic;

namespace Domain.Shared.Mapping
{
    public class ModelMappingRegistration : IMappingRegistration
    {
        public Dictionary<string, NodeMap> Register()
        {
            var mappings = new Dictionary<string, NodeMap>();
            
            //-----------------------------------------
            // CUSTOMER CUSTOMER RELATIONSHIP EDGE
            //-----------------------------------------
            var customerCustomerRelationshipEdge = new NodeMap
            {
                Id = 1,
                Schema = nameof(DataGraph.CustomerCustomerRelationshipEdge),
                IsGraph = true,
                IsModel = true
            };
            
            // customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
            // {
            //     SourceName = nameof(CustomerCustomerEdge.Clause),
            //     DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship)
            //     // ,
            //     // DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id)
            // });
            //
            // customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
            // {
            //     SourceName = nameof(CustomerCustomerEdge.LevelDepth),
            //     DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship)
            //     // ,
            //     // DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id)
            // });
            //
            // customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
            // {
            //     SourceName = nameof(CustomerCustomerEdge.LevelDirection),
            //     DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship)
            //     // ,
            //     // DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id)
            // });
            
            customerCustomerRelationshipEdge.Children.Add(nameof(DataEntity.CustomerCustomerRelationship));
            customerCustomerRelationshipEdge.Children.Add(nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer));
            customerCustomerRelationshipEdge.Children.Add(nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer));
            
            customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(DataGraph.CustomerCustomerRelationshipEdge.Id),
                DestinationEntity = nameof(DataGraph.CustomerCustomerRelationshipEdge),
                DestinationName = nameof(DataGraph.CustomerCustomerRelationshipEdge.Id)
            });
            
            customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(CustomerCustomerEdge.InnerCustomerKey),
                DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
                DestinationName = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey)
            });
            
            customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(CustomerCustomerEdge.OuterCustomerKey),
                DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
                DestinationName = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey)
            });
            
            customerCustomerRelationshipEdge.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(CustomerCustomerEdge.CustomerCustomerRelationshipKey),
                DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
                DestinationName = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)
            });

            GetMapping(mappings,
                MappingRegistry.Register(typeof(CustomerCustomerEdge), typeof(DataGraph.CustomerCustomerRelationshipEdge),
                    customerCustomerRelationshipEdge));

            //-----------------------------------------
            // CUSTOMER
            //-----------------------------------------
            var cust = new NodeMap
            {
                Id = 3,
                Schema = nameof(DataEntity.Schema.Banking)
            };

            cust.IsEntity = true;
            cust.IsModel = true;

            cust.Children.Add(nameof(DataEntity.ContactPoint));
            cust.Children.Add(nameof(DataEntity.CustomerBankingRelationship));

            cust.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.Customer), nameof(DataEntity.Customer.CustomerKey)));

            cust.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(DataEntity.Customer.Id),
                DestinationEntity = nameof(DataEntity.Customer),
                DestinationName = nameof(DataEntity.Customer.Id)
            });

            cust.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Customer.CustomerKey),
                DestinationEntity = nameof(DataEntity.Customer),
                DestinationName = nameof(DataEntity.Customer.CustomerKey)
            });

            cust.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Customer.FirstNaming),
                DestinationEntity = nameof(DataEntity.Customer),
                DestinationName = nameof(DataEntity.Customer.FirstName)
            });

            cust.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Customer.LastNaming),
                DestinationEntity = nameof(DataEntity.Customer),
                DestinationName = nameof(DataEntity.Customer.LastName)
            });

            cust.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Customer.FullNaming),
                DestinationEntity = nameof(DataEntity.Customer),
                DestinationName = nameof(DataEntity.Customer.FullName)
            });

            // Enum mapping for CustomerType
            var custEnums = EnumMapFactory.Create(
                new Dictionary<string, (CustomerType, DataEntity.CustomerType)>
                {
                    { $"{nameof(Customer)}~{nameof(DataEntity.Customer)}~{DataEntity.CustomerType.Person}", (CustomerType.Person, DataEntity.CustomerType.Person) },
                    { $"{nameof(Customer)}~{nameof(DataEntity.Customer)}~{DataEntity.CustomerType.Organisation}", (CustomerType.Organisation, DataEntity.CustomerType.Organisation) }
                },
                new Dictionary<string, (DataEntity.CustomerType, CustomerType)>
                {
                    { $"{nameof(DataEntity.Customer)}~{nameof(Customer)}~{CustomerType.Person}", (DataEntity.CustomerType.Person, CustomerType.Person) },
                    { $"{nameof(DataEntity.Customer)}~{nameof(Customer)}~{CustomerType.Organisation}", (DataEntity.CustomerType.Organisation, CustomerType.Organisation) }
                });

            cust.FromEnum = custEnums.from;
            cust.ToEnum = custEnums.to;

            // GetMapping(mappings, MappingRegistry.Register(typeof(Customer), typeof(DataEntity.Customer), cust), nameof(Customer));
            GetMapping(mappings, MappingRegistry.Register(typeof(Customer), typeof(DataEntity.Customer), cust, 
                nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer)), nameof(DataEntity.CustomerCustomerRelationship.InnerCustomer));
            GetMapping(mappings, MappingRegistry.Register(typeof(Customer), typeof(DataEntity.Customer), cust, 
                nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer)), nameof(DataEntity.CustomerCustomerRelationship.OuterCustomer));

            //-----------------------------------------
            // CONTACTPOINT
            //-----------------------------------------
            var cp = new NodeMap
            {
                Id = 4,
                Schema = nameof(DataEntity.Schema.Banking)
            };

            cp.IsEntity = true;
            cp.IsModel = true;

            cp.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.ContactPoint),
                nameof(DataEntity.ContactPoint.ContactPointKey)));

            cp.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(DataEntity.ContactPoint.Id),
                DestinationEntity = nameof(DataEntity.ContactPoint),
                DestinationName = nameof(DataEntity.ContactPoint.Id)
            });

            cp.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(ContactPoint.ContactPointKey),
                DestinationEntity = nameof(DataEntity.ContactPoint),
                DestinationName = nameof(DataEntity.ContactPoint.ContactPointKey)
            });

            cp.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(ContactPoint.ContactPointValue),
                DestinationEntity = nameof(DataEntity.ContactPoint),
                DestinationName = nameof(DataEntity.ContactPoint.ContactPointValue)
            });

            cp.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(ContactPoint.CustomerKey),
                DestinationEntity = nameof(DataEntity.ContactPoint),
                DestinationName = nameof(DataEntity.ContactPoint.CustomerKey)
            });

            // Enum mapping for ContactPointType
            var cpEnums = EnumMapFactory.Create(
                new Dictionary<string, (ContactPointType, DataEntity.ContactPointType)>
                {
                    { $"{nameof(ContactPoint)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContactPointType.Mobile}", (ContactPointType.Mobile, DataEntity.ContactPointType.Mobile) },
                    { $"{nameof(ContactPoint)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContactPointType.Landline}", (ContactPointType.Landline, DataEntity.ContactPointType.Landline) },
                    { $"{nameof(ContactPoint)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContactPointType.Email}", (ContactPointType.Email, DataEntity.ContactPointType.Email) }
                },
                new Dictionary<string, (DataEntity.ContactPointType, ContactPointType)>
                {
                    { $"{nameof(DataEntity.ContactPoint)}~{nameof(ContactPoint)}~{ContactPointType.Mobile}", (DataEntity.ContactPointType.Mobile, ContactPointType.Mobile) },
                    { $"{nameof(DataEntity.ContactPoint)}~{nameof(ContactPoint)}~{ContactPointType.Landline}", (DataEntity.ContactPointType.Landline, ContactPointType.Landline) },
                    { $"{nameof(DataEntity.ContactPoint)}~{nameof(ContactPoint)}~{ContactPointType.Email}", (DataEntity.ContactPointType.Email, ContactPointType.Email) }
                });

            cp.FromEnum = cpEnums.from;
            cp.ToEnum = cpEnums.to;

            GetMapping(mappings, MappingRegistry.Register(typeof(ContactPoint), typeof(DataEntity.ContactPoint), cp));

            //-----------------------------------------
            // CONTRACT
            //-----------------------------------------
            var contract = new NodeMap
            {
                Id = 6,
                Schema = nameof(DataEntity.Schema.Lending)
            };

            contract.IsEntity = true;

            contract.Children.Add(nameof(DataEntity.Account));
            contract.Children.Add(nameof(DataEntity.Transaction));

            contract.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.Contract),
                nameof(DataEntity.Contract.ContractKey)));

            contract.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(DataEntity.Contract.Id),
                DestinationEntity = nameof(DataEntity.Contract),
                DestinationName = nameof(DataEntity.Contract.Id)
            });

            contract.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Contract.ContractKey),
                DestinationEntity = nameof(DataEntity.Contract),
                DestinationName = nameof(DataEntity.Contract.ContractKey)
            });

            contract.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Contract.Amount),
                DestinationEntity = nameof(DataEntity.Contract),
                DestinationName = nameof(DataEntity.Contract.Amount)
            });
            
            // Enum mapping for ContractType
            var contractEnums = EnumMapFactory.Create(
                new Dictionary<string, (ProductType, DataEntity.ContractType)>
                {
                    { $"{nameof(Product)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContractType.CreditCard}", (ProductType.CreditCard, DataEntity.ContractType.CreditCard) },
                    { $"{nameof(Product)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContractType.Mortgage}", (ProductType.Mortgage, DataEntity.ContractType.Mortgage) },
                    { $"{nameof(Product)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContractType.PersonalLoan}", (ProductType.PersonalLoan, DataEntity.ContractType.PersonalLoan) }
                },
                new Dictionary<string, (DataEntity.ContractType, ProductType)>
                {
                    { $"{nameof(DataEntity.ContactPoint)}~{nameof(Product)}~{ProductType.CreditCard}", (DataEntity.ContractType.CreditCard, ProductType.CreditCard) },
                    { $"{nameof(DataEntity.ContactPoint)}~{nameof(Product)}~{ProductType.Mortgage}", (DataEntity.ContractType.Mortgage, ProductType.Mortgage) },
                    { $"{nameof(DataEntity.ContactPoint)}~{nameof(Product)}~{ProductType.PersonalLoan}", (DataEntity.ContractType.PersonalLoan, ProductType.PersonalLoan) }
                });

            contract.FromEnum = contractEnums.from;
            contract.ToEnum = contractEnums.to;

            GetMapping(mappings, MappingRegistry.Register(typeof(Contract), typeof(DataEntity.Contract), contract));

            //-----------------------------------------
            // ACCOUNT
            //-----------------------------------------
            var acct = new NodeMap
            {
                Id = 8,
                Schema = nameof(DataEntity.Schema.Account)
            };

            acct.IsEntity = true;

            acct.Children.Add(nameof(DataEntity.Contract));
            acct.Children.Add(nameof(DataEntity.Transaction));

            acct.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.Account), nameof(DataEntity.Account.AccountKey)));

            acct.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(DataEntity.Account.Id),
                DestinationEntity = nameof(DataEntity.Account),
                DestinationName = nameof(DataEntity.Account.Id)
            });

            acct.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Account.AccountKey),
                DestinationEntity = nameof(DataEntity.Account),
                DestinationName = nameof(DataEntity.Account.AccountKey)
            });

            acct.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Account.AccountNumber),
                DestinationEntity = nameof(DataEntity.Account),
                DestinationName = nameof(DataEntity.Account.AccountNumber)
            });

            acct.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Account.AccountName),
                DestinationEntity = nameof(DataEntity.Account),
                DestinationName = nameof(DataEntity.Account.AccountName)
            });

            GetMapping(mappings, MappingRegistry.Register(typeof(Account), typeof(DataEntity.Account), acct));

            //-----------------------------------------
            // CUSTOMERBANKINGRELATIONSHIP
            //-----------------------------------------
            var cbr = new NodeMap
            {
                Id = 5,
                Schema = nameof(DataEntity.Schema.Banking)
            };

            cbr.IsEntity = true;

            cbr.Children.Add(nameof(DataEntity.Contract));

            cbr.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.CustomerBankingRelationship),
                nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey)));

            cbr.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(DataEntity.CustomerBankingRelationship.Id),
                DestinationEntity = nameof(DataEntity.CustomerBankingRelationship),
                DestinationName = nameof(DataEntity.CustomerBankingRelationship.Id)
            });

            cbr.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(CustomerBankingRelationship.CustomerBankingRelationshipKey),
                DestinationEntity = nameof(DataEntity.CustomerBankingRelationship),
                DestinationName = nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey)
            });

            cbr.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(CustomerBankingRelationship.CustomerKey),
                DestinationEntity = nameof(DataEntity.CustomerBankingRelationship),
                DestinationName = nameof(DataEntity.CustomerBankingRelationship.CustomerKey)
            });

            GetMapping(mappings,
                MappingRegistry.Register(typeof(CustomerBankingRelationship),
                    typeof(DataEntity.CustomerBankingRelationship), cbr));

            //-----------------------------------------
            // CUSTOMERCUSTOMERRELATIONSHIP
            //-----------------------------------------
            var ccr = new NodeMap
            {
                Id = 2,
                Schema = nameof(DataEntity.Schema.Banking)
            };

            ccr.IsEntity = true;
            ccr.IsModel = true;

            ccr.Children.Add(nameof(DataEntity.Customer));

            ccr.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.CustomerCustomerRelationship),
                nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)));

            ccr.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(DataEntity.CustomerCustomerRelationship.Id),
                DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
                DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id)
            });

            ccr.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(DataEntity.CustomerCustomerRelationship.InnerCustomerKey),
                DestinationEntity = nameof(DataEntity.Customer),
                DestinationName = nameof(DataEntity.Customer.CustomerKey)
            });
            
            ccr.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(DataEntity.CustomerCustomerRelationship.OuterCustomerKey),
                DestinationEntity = nameof(DataEntity.Customer),
                DestinationName = nameof(DataEntity.Customer.CustomerKey)
            });

            ccr.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(DataEntity.CustomerCustomerRelationship.Id),
                DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
                DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id)
            });

            ccr.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(DataEntity.CustomerCustomerRelationship.Id),
                DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
                DestinationName = nameof(DataEntity.CustomerCustomerRelationship.Id)
            });

            ccr.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipKey),
                DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
                DestinationName = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipKey)
            });

            ccr.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(CustomerCustomerRelationship.CustomerCustomerRelationshipType),
                DestinationEntity = nameof(DataEntity.CustomerCustomerRelationship),
                DestinationName = nameof(DataEntity.CustomerCustomerRelationship.CustomerCustomerRelationshipType)
            });

            GetMapping(mappings,
                MappingRegistry.Register(typeof(CustomerCustomerRelationship),
                    typeof(DataEntity.CustomerCustomerRelationship), ccr));

            //-----------------------------------------
            // TRANSACTION
            //-----------------------------------------
            var transaction = new NodeMap
            {
                Id = 6,
                Schema = nameof(DataEntity.Schema.Lending)
            };

            transaction.IsEntity = true;
            transaction.IsModel = true;

            transaction.UpsertKeys.Add(new UpsertKey(nameof(DataEntity.Transaction),
                nameof(DataEntity.Transaction.TransactionKey)));

            transaction.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(DataEntity.Transaction.Id),
                DestinationEntity = nameof(DataEntity.Transaction),
                DestinationName = nameof(DataEntity.Transaction.Id)
            });

            transaction.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Transaction.TransactionKey),
                DestinationEntity = nameof(DataEntity.Transaction),
                DestinationName = nameof(DataEntity.Transaction.TransactionKey)
            });

            transaction.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Transaction.Amount),
                DestinationEntity = nameof(DataEntity.Transaction),
                DestinationName = nameof(DataEntity.Transaction.Amount)
            });

            transaction.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Transaction.Balance),
                DestinationEntity = nameof(DataEntity.Transaction),
                DestinationName = nameof(DataEntity.Transaction.Balance)
            });

            GetMapping(mappings,
                MappingRegistry.Register(typeof(Transaction), typeof(DataEntity.Transaction), transaction));

            //-----------------------------------------
            // PRODUCT
            //-----------------------------------------
            var product = new NodeMap
            {
                Id = 8,
                Schema = nameof(DataEntity.Schema.Banking)
            };

            product.IsModel = true;

            product.Children.Add(nameof(Contract));
            product.Children.Add(nameof(Account));

            product.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Product.ContractKey),
                DestinationEntity = nameof(DataEntity.Contract),
                DestinationName = nameof(DataEntity.Contract.ContractKey)
            });

            product.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Product.CustomerKey),
                DestinationEntity = nameof(DataEntity.Customer),
                DestinationName = nameof(DataEntity.Customer.CustomerKey)
            });

            product.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Product.AccountKey),
                DestinationEntity = nameof(DataEntity.Account),
                DestinationName = nameof(DataEntity.Account.AccountKey)
            });

            product.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Product.AccountName),
                DestinationEntity = nameof(DataEntity.Account),
                DestinationName = nameof(DataEntity.Account.AccountName)
            });

            product.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Product.AccountNumber),
                DestinationEntity = nameof(DataEntity.Account),
                DestinationName = nameof(DataEntity.Account.AccountNumber)
            });

            // Enum mapping for ProductType by value
            var productEnums = EnumMapFactory.Create(
                new Dictionary<string, (ProductType, DataEntity.ContractType)>
                {
                    { $"{nameof(Product)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContractType.CreditCard}", (ProductType.CreditCard, DataEntity.ContractType.CreditCard) },
                    { $"{nameof(Product)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContractType.Mortgage}", (ProductType.Mortgage, DataEntity.ContractType.Mortgage) },
                    { $"{nameof(Product)}~{nameof(DataEntity.ContactPoint)}~{DataEntity.ContractType.PersonalLoan}", (ProductType.PersonalLoan, DataEntity.ContractType.PersonalLoan) }
                },
                new Dictionary<string, (DataEntity.ContractType, ProductType)>
                {
                    { $"{nameof(DataEntity.ContactPoint)}~{nameof(Product)}~{ProductType.CreditCard}", (DataEntity.ContractType.CreditCard, ProductType.CreditCard) },
                    { $"{nameof(DataEntity.ContactPoint)}~{nameof(Product)}~{ProductType.Mortgage}", (DataEntity.ContractType.Mortgage, ProductType.Mortgage) },
                    { $"{nameof(DataEntity.ContactPoint)}~{nameof(Product)}~{ProductType.PersonalLoan}", (DataEntity.ContractType.PersonalLoan, ProductType.PersonalLoan) }
                });

            product.FromEnum = productEnums.from;
            product.ToEnum = productEnums.to;

            GetMapping(mappings, MappingRegistry.Register(typeof(Product), null, product));
            
            return mappings;
        }

        private void GetMapping(Dictionary<string, NodeMap> mappings, NodeMap nodeMap, string alias = "")
        {
            if (string.IsNullOrEmpty(alias))
            {
                alias = nodeMap.ModelType.Name;
            }
            
            if (nodeMap.ModelType != null)
            {
                mappings.TryAdd(alias, nodeMap);
            }
            
            if (nodeMap.EntityType != null)
            {
                mappings.TryAdd(alias, nodeMap);    
            }
        }
    }
}