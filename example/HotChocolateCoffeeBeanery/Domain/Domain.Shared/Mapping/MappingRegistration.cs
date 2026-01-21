using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using DataEntity = Database.Entity;
using Domain.Model;

namespace Domain.Shared.Mapping
{
    public static class MappingRegistration
    {
        public enum Schema
        {
            Banking,
            Lending,
            Account
        }

        public static void Register()
        {
            //-----------------------------------------
            // CUSTOMER
            //-----------------------------------------
            MappingRegistry.Register(
                MappingBuilder<Customer>.Create(m =>
                {
                    var cust = new EntityMap<Customer, DataEntity.Customer>();
                    cust.Schema = Schema.Banking.ToString();
                    cust.UpsertKeys.Add(
                        new UpsertKey(nameof(DataEntity.Customer),
                                      nameof(DataEntity.Customer.CustomerKey)));

                    cust.FieldMaps.Add(new FieldMap<Customer, DataEntity.Customer>
                    {
                        Source = c => c.CustomerKey,
                        Destination = e => e.CustomerKey
                    });

                    cust.FieldMaps.Add(new FieldMap<Customer, DataEntity.Customer>
                    {
                        Source = c => c.FirstNaming,
                        Destination = e => e.FirstName
                    });

                    cust.FieldMaps.Add(new FieldMap<Customer, DataEntity.Customer>
                    {
                        Source = c => c.LastNaming,
                        Destination = e => e.LastName
                    });

                    cust.FieldMaps.Add(new FieldMap<Customer, DataEntity.Customer>
                    {
                        Source = c => c.FullNaming,
                        Destination = e => e.FullName
                    });

                    cust.EnumMaps.Add(EnumMapFactory.Create(
                        new Dictionary<CustomerType, DataEntity.CustomerType>
                        {
                            { CustomerType.Person, DataEntity.CustomerType.Person },
                            { CustomerType.Organisation, DataEntity.CustomerType.Organisation }
                        },
                        new Dictionary<DataEntity.CustomerType, CustomerType>
                        {
                            { DataEntity.CustomerType.Person, CustomerType.Person },
                            { DataEntity.CustomerType.Organisation, CustomerType.Organisation }
                        }));

                    m.AddEntityMap(cust);
                }).Build());

            //-----------------------------------------
            // CONTACTPOINT
            //-----------------------------------------
            MappingRegistry.Register(
                MappingBuilder<ContactPoint>.Create(m =>
                {
                    var cp = new EntityMap<ContactPoint, DataEntity.ContactPoint>();
                    cp.Schema = Schema.Banking.ToString();
                    cp.UpsertKeys.Add(
                        new UpsertKey(nameof(DataEntity.ContactPoint),
                                      nameof(DataEntity.ContactPoint.ContactPointKey)));

                    cp.FieldMaps.Add(new FieldMap<ContactPoint, DataEntity.ContactPoint>
                    {
                        Source = x => x.ContactPointKey,
                        Destination = e => e.ContactPointKey
                    });

                    cp.FieldMaps.Add(new FieldMap<ContactPoint, DataEntity.ContactPoint>
                    {
                        Source = x => x.ContactPointValue,
                        Destination = e => e.ContactPointValue
                    });

                    cp.FieldMaps.Add(new FieldMap<ContactPoint, DataEntity.ContactPoint>
                    {
                        Source = x => x.CustomerKey,
                        Destination = e => e.CustomerKey
                    });

                    cp.EnumMaps.Add(EnumMapFactory.Create(
                        new Dictionary<ContactPointType, DataEntity.ContactPointType>
                        {
                            { ContactPointType.Mobile, DataEntity.ContactPointType.Mobile },
                            { ContactPointType.Landline, DataEntity.ContactPointType.Landline },
                            { ContactPointType.Email, DataEntity.ContactPointType.Email }
                        },
                        new Dictionary<DataEntity.ContactPointType, ContactPointType>
                        {
                            { DataEntity.ContactPointType.Mobile, ContactPointType.Mobile },
                            { DataEntity.ContactPointType.Landline, ContactPointType.Landline },
                            { DataEntity.ContactPointType.Email, ContactPointType.Email }
                        }));

                    cp.LinkMaps.Add(new LinkMap<ContactPoint, DataEntity.ContactPoint>
                    {
                        SourceKey = x => x.CustomerKey,
                        EntityKey = e => e.CustomerKey
                    });

                    m.AddEntityMap(cp);
                }).Build());

            //-----------------------------------------
            // ACCOUNT
            //-----------------------------------------
            MappingRegistry.Register(
                MappingBuilder<Account>.Create(m =>
                {
                    var acct = new EntityMap<Account, DataEntity.Account>();
                    acct.Schema = Schema.Account.ToString();
                    acct.UpsertKeys.Add(
                        new UpsertKey(nameof(DataEntity.Account),
                                      nameof(DataEntity.Account.AccountKey)));

                    acct.FieldMaps.Add(new FieldMap<Account, DataEntity.Account>
                    {
                        Source = x => x.AccountKey,
                        Destination = e => e.AccountKey
                    });

                    acct.FieldMaps.Add(new FieldMap<Account, DataEntity.Account>
                    {
                        Source = x => x.AccountNumber,
                        Destination = e => e.AccountNumber
                    });

                    acct.FieldMaps.Add(new FieldMap<Account, DataEntity.Account>
                    {
                        Source = x => x.AccountName,
                        Destination = e => e.AccountName
                    });

                    m.AddEntityMap(acct);
                }).Build());

            //-----------------------------------------
            // CONTRACT
            //-----------------------------------------
            MappingRegistry.Register(
                MappingBuilder<Contract>.Create(m =>
                {
                    var ct = new EntityMap<Contract, DataEntity.Contract>();
                    ct.Schema = Schema.Lending.ToString();
                    ct.UpsertKeys.Add(
                        new UpsertKey(nameof(DataEntity.Contract),
                                      nameof(DataEntity.Contract.ContractKey)));

                    ct.FieldMaps.Add(new FieldMap<Contract, DataEntity.Contract>
                    {
                        Source = x => x.ContractKey,
                        Destination = e => e.ContractKey
                    });

                    ct.FieldMaps.Add(new FieldMap<Contract, DataEntity.Contract>
                    {
                        Source = x => x.Amount,
                        Destination = e => e.Amount
                    });

                    ct.EnumMaps.Add(EnumMapFactory.Create(
                        new Dictionary<ContractType, DataEntity.ContractType>
                        {
                            { ContractType.CreditCard, DataEntity.ContractType.CreditCard },
                            { ContractType.Mortgage, DataEntity.ContractType.Mortgage },
                            { ContractType.PersonalLoan, DataEntity.ContractType.PersonalLoan }
                        },
                        new Dictionary<DataEntity.ContractType, ContractType>
                        {
                            { DataEntity.ContractType.CreditCard, ContractType.CreditCard },
                            { DataEntity.ContractType.Mortgage, ContractType.Mortgage },
                            { DataEntity.ContractType.PersonalLoan, ContractType.PersonalLoan }
                        }));

                    m.AddEntityMap(ct);
                }).Build());

            //-----------------------------------------
            // CUSTOMERBANKINGRELATIONSHIP
            //-----------------------------------------
            MappingRegistry.Register(
                MappingBuilder<CustomerBankingRelationship>.Create(m =>
                {
                    var cbr = new EntityMap<CustomerBankingRelationship, DataEntity.CustomerBankingRelationship>();
                    cbr.Schema = Schema.Banking.ToString();
                    cbr.UpsertKeys.Add(
                        new UpsertKey(nameof(DataEntity.CustomerBankingRelationship),
                                      nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey)));

                    cbr.FieldMaps.Add(new FieldMap<CustomerBankingRelationship, DataEntity.CustomerBankingRelationship>
                    {
                        Source = x => x.CustomerBankingRelationshipKey,
                        Destination = e => e.CustomerBankingRelationshipKey
                    });

                    cbr.FieldMaps.Add(new FieldMap<CustomerBankingRelationship, DataEntity.CustomerBankingRelationship>
                    {
                        Source = x => x.CustomerKey,
                        Destination = e => e.CustomerKey
                    });

                    cbr.LinkMaps.Add(new LinkMap<CustomerBankingRelationship, DataEntity.CustomerBankingRelationship>
                    {
                        SourceKey = x => x.CustomerKey,
                        EntityKey = e => e.CustomerKey
                    });

                    m.AddEntityMap(cbr);
                }).Build());
        }
    }
}
