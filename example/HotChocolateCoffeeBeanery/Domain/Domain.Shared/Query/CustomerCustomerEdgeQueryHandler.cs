using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.Service;
using Domain.Model;
using DataEntity = Database.Entity;
using Npgsql;
using System;
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

            var wrapper = new Wrapper { CustomerCustomerEdge = new List<CustomerCustomerEdge>() };
            int totalCount = 0;
            int pageRecords = 0;

            if (map == null || map.Length == 0)
                return (new List<M>(), 0, 0, 0, 0);

            // Create a single edge for this row
            var edge = new CustomerCustomerEdge();
            wrapper.CustomerCustomerEdge.Add(edge);

            int i = 0;
            foreach (var kvp in sqlStructure.EntityMapping)
            {
                var alias = kvp.Key;
                var type = kvp.Value;

                if (i >= map.Length)
                    break;

                var entity = map[i++];
                if (entity == null)
                {
                    Console.WriteLine($"NULL ENTITY for alias {alias}");
                    continue;
                }

                Console.WriteLine($"Mapping alias '{alias}' for entity type '{entity.GetType().Name}'");

                var mappedModel = _mapper.MapByAlias(type, entity, alias);

                if (mappedModel == null)
                {
                    Console.WriteLine($"MapByAlias returned null for alias {alias}");
                    continue;
                }

                switch (alias)
                {
                    case "CustomerCustomerRelationship":
                        edge.CustomerCustomerRelationship = mappedModel as CustomerCustomerRelationship;
                        edge.CustomerCustomerRelationshipKey = GetPropertyValue<Guid?>(entity, "CustomerCustomerRelationshipKey");
                        edge.InnerCustomerKey = GetPropertyValue<Guid?>(entity, "InnerCustomerKey");
                        edge.OuterCustomerKey = GetPropertyValue<Guid?>(entity, "OuterCustomerKey");
                        break;

                    case "InnerCustomer":
                        edge.InnerCustomer = mappedModel as Customer;
                        break;

                    case "OuterCustomer":
                        edge.OuterCustomer = mappedModel as Customer;
                        break;

                    // Add other cases if needed
                    default:
                        Console.WriteLine($"Alias '{alias}' not handled explicitly");
                        break;
                }
            }

            Console.WriteLine("=== MAPPING CONFIGURATION END ===");

            var result = new List<M> { (M)(object)wrapper };
            return (result, sqlStructure.Pagination?.StartCursor, sqlStructure.Pagination?.EndCursor, totalCount, pageRecords);
        }

        private static T? GetPropertyValue<T>(object obj, string propertyName)
        {
            if (obj == null) return default;
            var prop = obj.GetType().GetProperty(propertyName);
            return prop != null ? (T?)prop.GetValue(obj) : default;
        }
    }
}