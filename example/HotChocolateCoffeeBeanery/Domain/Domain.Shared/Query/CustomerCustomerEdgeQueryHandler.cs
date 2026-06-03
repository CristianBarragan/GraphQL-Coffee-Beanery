using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.Service;
using Domain.Model;
using Npgsql;

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
            var customerCustomerEdges = existingWrappers
                .SelectMany(w => w.CustomerCustomerEdge ?? new List<CustomerCustomerEdge>())
                .ToList();

            var totalCount  = 0;
            var pageRecords = 0;
            var customerCustomerEdge = new CustomerCustomerEdge();

            for (int i = 0; i < map.Length; i++)
            {
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

                    var matchingAliases = MappingRegistry.Registry
                        .Where(kvp =>
                            kvp.Value.EntityType == incomingType &&
                            kvp.Value.ModelType  == targetModelType)
                        .Select(kvp => kvp.Key)
                        .ToList();

                    if (!matchingAliases.Any())
                    {
                        matchingAliases = MappingRegistry.Registry
                            .Where(kvp =>
                                kvp.Value.EntityType?.Name.Equals(
                                    incomingType.Name, StringComparison.OrdinalIgnoreCase) == true)
                            .Select(kvp => kvp.Key)
                            .ToList();
                    }

                    if (!matchingAliases.Any())
                    {
                        matchingAliases = MappingRegistry.Registry.Keys
                            .Where(k => k.EndsWith(
                                incomingType.Name, StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    }

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

            var existingIndex = customerCustomerEdges.FindIndex(c =>
                c.InnerCustomerKey == customerCustomerEdge.InnerCustomerKey);

            if (existingIndex >= 0)
                customerCustomerEdges[existingIndex] = customerCustomerEdge;
            else
                customerCustomerEdges.Add(customerCustomerEdge);
            
            var wrapper = existingWrappers.FirstOrDefault() ?? new Wrapper();
            wrapper.CustomerCustomerEdge = customerCustomerEdges;

            var wrappers = new List<Wrapper> { wrapper };

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