using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.Service;
using Domain.Model;
using DataEntity = Database.Entity;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

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

        public override (List<M> models, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)
            MappingConfiguration(List<M> models, SqlStructure sqlStructure, object[] map, List<Type> types)
        {
            var customerCustomerEdges = models.OfType<CustomerCustomerEdge>().ToList();
            var totalCount  = 0;
            var pageRecords = 0;
            var customerCustomerEdge = new CustomerCustomerEdge();

            for (int i = 0; i < map.Length; i++)
            {
                if (map[i] is TotalPageRecords totalPageRecords)
                {
                    pageRecords = totalPageRecords.PageRecords;
                }
                else if (map[i] is TotalRecordCount totalRecordCount)
                {
                    totalCount = totalRecordCount.RecordCount;
                }
                else
                {
                    var incomingType = map[i].GetType();
                    var targetModelType = types[i];

                    // Resolve alias from registry matching both EntityType and ModelType
                    var alias = MappingRegistry.Registry
                        .Where(kvp =>
                            kvp.Value.EntityType == incomingType &&
                            kvp.Value.ModelType  == targetModelType)
                        .Select(kvp => kvp.Key)
                        .FirstOrDefault();

                    // Resolve idPropertyName dynamically from the NodeMap for this alias —
                    // find the first FieldMap whose DestinationEntity matches the incoming
                    // entity type and whose DestinationName is "Id" or falls back to the
                    // first available FieldMap destination as the unique key
                    var idPropertyName = ResolveIdPropertyName(incomingType, alias);

                    customerCustomerEdge = (CustomerCustomerEdge)_mapper.MapDynamicToModel(
                        customerCustomerEdge,   // dynamic source  — existing TModel to preserve
                        targetModelType,        // Type            — target model type
                        map[i],                 // object current  — incoming entity
                        idPropertyName,         // string id       — resolved per entity type
                        alias                   // string alias    — resolved NodeMap key
                    );
                }
            }

            var existingCustomerIndex = customerCustomerEdges.FindIndex(c =>
                c.InnerCustomerKey == customerCustomerEdge.InnerCustomerKey);

            if (existingCustomerIndex >= 0)
                customerCustomerEdges[existingCustomerIndex] = customerCustomerEdge;
            else
                customerCustomerEdges.Add(customerCustomerEdge);

            dynamic list = customerCustomerEdges;
            return (list, sqlStructure.Pagination?.StartCursor, sqlStructure.Pagination?.EndCursor,
                totalCount, pageRecords);
        }

        /// <summary>
        /// Resolves the unique ID property name on the incoming entity type by:
        /// 1. Looking for a FieldMap with DestinationName == "Id" in the aliased NodeMap
        /// 2. Falling back to the first FieldMap destination in the NodeMap
        /// 3. Falling back to "Id" by convention if no NodeMap is found
        /// </summary>
        private static string ResolveIdPropertyName(Type incomingEntityType, string? alias)
        {
            if (alias != null && MappingRegistry.Registry.TryGetValue(alias, out var nodeMap))
            {
                // Prefer a FieldMap explicitly named "Id" on the incoming entity side
                var idFieldMap = nodeMap.FieldMaps
                    .FirstOrDefault(f =>
                        f.DestinationEntity.Equals(incomingEntityType.Name, StringComparison.OrdinalIgnoreCase) &&
                        f.DestinationName.Equals("Id", StringComparison.OrdinalIgnoreCase));

                if (idFieldMap != null)
                    return idFieldMap.DestinationName;

                // Fall back to first FieldMap for this entity as the key
                var fallbackFieldMap = nodeMap.FieldMaps
                    .FirstOrDefault(f =>
                        f.DestinationEntity.Equals(incomingEntityType.Name, StringComparison.OrdinalIgnoreCase));

                if (fallbackFieldMap != null)
                    return fallbackFieldMap.DestinationName;
            }

            // Final convention fallback — most entities have an "Id" property
            return "Id";
        }
    }
}