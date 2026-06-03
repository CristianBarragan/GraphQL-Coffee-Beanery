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
            // Unwrap existing Wrapper instances to get their CustomerCustomerEdge lists
            var existingWrappers = models.OfType<Wrapper>().ToList();
            var customerCustomerEdges = existingWrappers
                .SelectMany(w => w.CustomerCustomerEdge ?? new List<CustomerCustomerEdge>())
                .ToList();

            var totalCount  = 0;
            var pageRecords = 0;
            var customerCustomerEdge = new CustomerCustomerEdge();

            for (int i = 0; i < map.Length; i++)
            {
                Console.WriteLine($"[MAP] [{i}] type={map[i]?.GetType().Name ?? "null"} value={map[i]}");
                Console.WriteLine($"[MAP] [{i}] types[i]={types[i]?.Name ?? "null"}");
                
                if (map[i] == null)
                {
                    continue;
                } 
                
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
                    var incomingType    = map[i].GetType();
                    var targetModelType = types[i];

                    // Step 1 — exact match on both EntityType and ModelType
                    var matchingAliases = MappingRegistry.Registry
                        .Where(kvp =>
                            kvp.Value.EntityType == incomingType &&
                            kvp.Value.ModelType  == targetModelType)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    // Step 2 — fallback: match by EntityType name only
                    // handles cases like CustomerCustomerRelationship where model == entity
                    if (!matchingAliases.Any())
                    {
                        matchingAliases = MappingRegistry.Registry
                            .Where(kvp =>
                                kvp.Value.EntityType?.Name.Equals(
                                    incomingType.Name, StringComparison.OrdinalIgnoreCase) == true)
                            .Select(kvp => kvp.Key)
                            .ToList();
                    }

                    // Step 3 — fallback: match by registry key suffix
                    if (!matchingAliases.Any())
                    {
                        matchingAliases = MappingRegistry.Registry.Keys
                            .Where(k => k.EndsWith(
                                incomingType.Name, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

                    // FIXED: iterate ALL matching aliases so every mapped property is applied
                    // e.g. InnerCustomerCustomerCustomerRelationship AND
                    // OuterCustomerCustomerCustomerRelationship both contribute their
                    // fields to customerCustomerEdge
                    foreach (var alias in matchingAliases)
                    {
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
            }

            // Find existing CustomerCustomerEdge by InnerCustomerKey and upsert
            var existingIndex = customerCustomerEdges.FindIndex(c =>
                c.InnerCustomerKey == customerCustomerEdge.InnerCustomerKey);

            if (existingIndex >= 0)
                customerCustomerEdges[existingIndex] = customerCustomerEdge;
            else
                customerCustomerEdges.Add(customerCustomerEdge);

            // Wrap CustomerCustomerEdge list back into a single Wrapper instance
            // M is Wrapper so we need List<Wrapper> not List<CustomerCustomerEdge>
            var wrapper = existingWrappers.FirstOrDefault() ?? new Wrapper();
            wrapper.CustomerCustomerEdge = customerCustomerEdges;

            var wrappers = new List<Wrapper> { wrapper };

            dynamic list = wrappers;
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
                        f.DestinationEntity.Equals(incomingEntityType.Name,
                            StringComparison.OrdinalIgnoreCase) &&
                        f.DestinationName.Equals("Id", StringComparison.OrdinalIgnoreCase));

                if (idFieldMap != null)
                    return idFieldMap.DestinationName;

                // Fall back to first FieldMap for this entity as the key
                var fallbackFieldMap = nodeMap.FieldMaps
                    .FirstOrDefault(f =>
                        f.DestinationEntity.Equals(incomingEntityType.Name,
                            StringComparison.OrdinalIgnoreCase));

                if (fallbackFieldMap != null)
                    return fallbackFieldMap.DestinationName;
            }

            // Final convention fallback — most entities have an "Id" property
            return "Id";
        }
    }
}