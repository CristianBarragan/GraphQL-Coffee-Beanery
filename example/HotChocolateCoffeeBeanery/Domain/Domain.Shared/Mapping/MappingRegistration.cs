using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.Mapping;
using CoffeeBeanery.GraphQL.Core.Sql;
using DataEntity = Database.Entity;
using Domain.Model;

namespace Domain.Shared.Mapping
{
    public class MappingRegistration : IMappingRegistration
    {
        public enum Schema
        {
            Banking,
            Lending,
            Account
        }

        public void Register()
        {
            //-----------------------------------------
            // CUSTOMER
            //-----------------------------------------
            var cust = new EntityMap
            {
                Schema = Schema.Banking.ToString()
            };

            cust.UpsertKeys.Add(
                new UpsertKey(nameof(DataEntity.Customer),
                              nameof(DataEntity.Customer.CustomerKey)));

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

            // Enum maps
            var custEnums = EnumMapFactory.Create(
                new Dictionary<CustomerType, DataEntity.CustomerType>
                {
                    { CustomerType.Person, DataEntity.CustomerType.Person },
                    { CustomerType.Organisation, DataEntity.CustomerType.Organisation }
                },
                new Dictionary<DataEntity.CustomerType, CustomerType>
                {
                    { DataEntity.CustomerType.Person, CustomerType.Person },
                    { DataEntity.CustomerType.Organisation, CustomerType.Organisation }
                });

            cust.FromEnum = custEnums.from;
            cust.ToEnum = custEnums.to;

            MappingRegistry.Register(nameof(Customer), cust);


            //-----------------------------------------
            // CONTACTPOINT
            //-----------------------------------------
            var cp = new EntityMap
            {
                Schema = Schema.Banking.ToString()
            };

            cp.UpsertKeys.Add(
                new UpsertKey(nameof(DataEntity.ContactPoint),
                              nameof(DataEntity.ContactPoint.ContactPointKey)));

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

            var cpEnums = EnumMapFactory.Create(
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
                });

            cp.FromEnum = cpEnums.from;
            cp.ToEnum = cpEnums.to;

            MappingRegistry.Register(nameof(ContactPoint), cp);


            //-----------------------------------------
            // ACCOUNT
            //-----------------------------------------
            var acct = new EntityMap
            {
                Schema = Schema.Account.ToString()
            };

            acct.UpsertKeys.Add(
                new UpsertKey(nameof(DataEntity.Account),
                              nameof(DataEntity.Account.AccountKey)));

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

            MappingRegistry.Register(nameof(Account), acct);


            //-----------------------------------------
            // CONTRACT
            //-----------------------------------------
            var ct = new EntityMap
            {
                Schema = Schema.Lending.ToString()
            };

            ct.UpsertKeys.Add(
                new UpsertKey(nameof(DataEntity.Contract),
                              nameof(DataEntity.Contract.ContractKey)));

            ct.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Contract.ContractKey),
                DestinationEntity = nameof(DataEntity.Contract),
                DestinationName = nameof(DataEntity.Contract.ContractKey)
            });

            ct.FieldMaps.Add(new FieldMap
            {
                SourceName = nameof(Contract.Amount),
                DestinationEntity = nameof(DataEntity.Contract),
                DestinationName = nameof(DataEntity.Contract.Amount)
            });

            var ctEnums = EnumMapFactory.Create(
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
                });

            ct.FromEnum = ctEnums.from;
            ct.ToEnum = ctEnums.to;

            MappingRegistry.Register(nameof(Contract), ct);


            //-----------------------------------------
            // CUSTOMERBANKINGRELATIONSHIP
            //-----------------------------------------
            var cbr = new EntityMap
            {
                Schema = Schema.Banking.ToString()
            };

            cbr.UpsertKeys.Add(
                new UpsertKey(nameof(DataEntity.CustomerBankingRelationship),
                              nameof(DataEntity.CustomerBankingRelationship.CustomerBankingRelationshipKey)));

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

            MappingRegistry.Register(nameof(CustomerBankingRelationship), cbr);
        }
    }
}
