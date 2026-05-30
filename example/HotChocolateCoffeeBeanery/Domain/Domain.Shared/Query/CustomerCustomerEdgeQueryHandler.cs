// CustomerCustomerEdgeQueryHandler.cs
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
            var existingWrappers = models.OfType<Wrapper>().ToList();
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
                    if (map[i] == null)
                    {
                        continue;
                    }
                    
                    var incomingType    = map[i].GetType();
                    var targetModelType = types[i];

                    // Step 1 — exact match on both EntityType and ModelType
                    var alias = MappingRegistry.Registry
                        .Where(kvp =>
                            kvp.Value.EntityType == incomingType &&
                            kvp.Value.ModelType  == targetModelType)
                        .Select(kvp => kvp.Key)
                        .FirstOrDefault();

                    // Step 2 — fallback: match by EntityType name only
                    // handles cases like CustomerCustomerRelationship where model == entity
                    if (alias == null)
                    {
                        alias = MappingRegistry.Registry
                            .Where(kvp =>
                                kvp.Value.EntityType?.Name.Equals(
                                    incomingType.Name, StringComparison.OrdinalIgnoreCase) == true)
                            .Select(kvp => kvp.Key)
                            .FirstOrDefault();
                    }

                    // Step 3 — fallback: match by registry key suffix
                    if (alias == null)
                    {
                        alias = MappingRegistry.Registry.Keys
                            .FirstOrDefault(k => k.EndsWith(
                                incomingType.Name, StringComparison.OrdinalIgnoreCase));
                    }

                    var idPropertyName = ResolveIdPropertyName(incomingType, alias);

                    customerCustomerEdge = (CustomerCustomerEdge)_mapper.MapDynamicToModel(
                        customerCustomerEdge,
                        targetModelType,
                        map[i],
                        idPropertyName,
                        alias
                    );
                }
            }

            var existingCustomerIndex = customerCustomerEdges.FindIndex(c =>
                c.InnerCustomerKey == customerCustomerEdge.InnerCustomerKey);

            if (existingCustomerIndex >= 0)
                customerCustomerEdges[existingCustomerIndex] = customerCustomerEdge;
            else
                customerCustomerEdges.Add(customerCustomerEdge);

            var wrapper = existingWrappers.FirstOrDefault() ?? new Wrapper();
            wrapper.CustomerCustomerEdge = customerCustomerEdges;

            var wrappers = new List<Wrapper> { wrapper };

            // Cast back to List<M> via dynamic
            dynamic list = wrappers;
            return (list, sqlStructure.Pagination?.StartCursor, sqlStructure.Pagination?.EndCursor,
                totalCount, pageRecords);
        }

        private static string ResolveIdPropertyName(Type incomingEntityType, string? alias)
        {
            if (alias != null && MappingRegistry.Registry.TryGetValue(alias, out var nodeMap))
            {
                var idFieldMap = nodeMap.FieldMaps
                    .FirstOrDefault(f =>
                        f.DestinationEntity.Equals(incomingEntityType.Name,
                            StringComparison.OrdinalIgnoreCase) &&
                        f.DestinationName.Equals("Id", StringComparison.OrdinalIgnoreCase));

                if (idFieldMap != null)
                    return idFieldMap.DestinationName;

                var fallbackFieldMap = nodeMap.FieldMaps
                    .FirstOrDefault(f =>
                        f.DestinationEntity.Equals(incomingEntityType.Name,
                            StringComparison.OrdinalIgnoreCase));

                if (fallbackFieldMap != null)
                    return fallbackFieldMap.DestinationName;
            }

            return "Id";
        }
    }
}