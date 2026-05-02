using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.Service;
using Domain.Model;
using DataEntity = Database.Entity;
using Npgsql;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Shared.Query
{
    public class CustomerCustomerEdgeQueryHandler<M> : ProcessQuery<M>,
        IQuery<ProcessQueryParameters,
        (List<M> list, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>
        where M : class
    {
        private readonly IMapper _mapper;

        public CustomerCustomerEdgeQueryHandler(
            ILoggerFactory loggerFactory,
            NpgsqlConnection dbConnection,
            IMapper mapper)
            : base(loggerFactory, dbConnection)
        {
            _mapper = mapper;
        }

        public override (List<M>, int?, int?, int?, int?)
            MappingConfiguration(List<M> models, SqlStructure sqlStructure, object[] map)
        {
            Console.WriteLine("=== MAPPING CONFIGURATION START ===");

            var wrapper = new Wrapper
            {
                CustomerCustomerEdge = new List<CustomerCustomerEdge>()
            };

            int totalCount = 0;
            int pageRecords = 0;

            if (map == null || map.Length == 0)
                return (new List<M>(), 0, 0, 0, 0);

            foreach (var item in map)
            {
                if (item == null)
                {
                    Console.WriteLine("NULL ITEM FROM DAPPER");
                    continue;
                }

                Console.WriteLine($"Map item type: {item.GetType().FullName}");

                if (item is TotalPageRecords tpr)
                {
                    pageRecords = tpr.PageRecords;
                    continue;
                }

                if (item is TotalRecordCount trc)
                {
                    totalCount = trc.RecordCount;
                    continue;
                }

                // 🔥 Wrap the entity dynamically using EntityMapping
                if (item is DataEntity.CustomerCustomerRelationship ccrEntity)
                {
                    var edge = wrapper.CustomerCustomerEdge
                        .FirstOrDefault(e => e.CustomerCustomerRelationshipKey == ccrEntity.CustomerCustomerRelationshipKey);

                    if (edge == null)
                    {
                        edge = new CustomerCustomerEdge
                        {
                            CustomerCustomerRelationship = _mapper.MapByAlias(typeof(DataEntity.CustomerCustomerRelationship), ccrEntity, "CustomerCustomerRelationship") as CustomerCustomerRelationship,
                            CustomerCustomerRelationshipKey = ccrEntity.CustomerCustomerRelationshipKey,
                            InnerCustomerKey = ccrEntity.InnerCustomerKey,
                            OuterCustomerKey = ccrEntity.OuterCustomerKey
                        };
                        wrapper.CustomerCustomerEdge.Add(edge);
                        Console.WriteLine("Added new CustomerCustomerEdge to wrapper");
                    }

                    // Map InnerCustomer dynamically
                    if (sqlStructure.EntityMapping.TryGetValue("InnerCustomer", out var innerType))
                    {
                        var innerEntity = GetPropertyValue<object>(ccrEntity, "InnerCustomer");
                        if (innerEntity != null)
                            edge.InnerCustomer = _mapper.MapByAlias(innerType, innerEntity, "InnerCustomer") as Customer;
                    }

                    // Map OuterCustomer dynamically
                    if (sqlStructure.EntityMapping.TryGetValue("OuterCustomer", out var outerType))
                    {
                        var outerEntity = GetPropertyValue<object>(ccrEntity, "OuterCustomer");
                        if (outerEntity != null)
                            edge.OuterCustomer = _mapper.MapByAlias(outerType, outerEntity, "OuterCustomer") as Customer;
                    }
                }
                else
                {
                    Console.WriteLine($"Unknown entity type: {item.GetType().Name}, skipping wrap.");
                }
            }

            Console.WriteLine("=== MAPPING CONFIGURATION END ===");

            var result = new List<M> { (M)(object)wrapper };

            return (
                result,
                sqlStructure.Pagination?.StartCursor,
                sqlStructure.Pagination?.EndCursor,
                totalCount,
                pageRecords
            );
        }

        // Helper to dynamically get property values using reflection
        private static T? GetPropertyValue<T>(object obj, string propertyName)
        {
            if (obj == null) return default;
            var prop = obj.GetType().GetProperty(propertyName);
            return prop != null ? (T?)prop.GetValue(obj) : default;
        }
    }
}