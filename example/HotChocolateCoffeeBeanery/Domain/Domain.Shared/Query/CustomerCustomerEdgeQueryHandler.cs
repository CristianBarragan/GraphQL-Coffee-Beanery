
using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.Service;
using Domain.Model;
using Npgsql;

namespace Domain.Shared.Query;

public class CustomerCustomerEdgeQueryHandler<M> : ProcessQuery<M>, IQuery<ProcessQueryParameters,
    (List<M> list, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>
    where M : class
{
    private readonly IMapper _mapper;
    
    public CustomerCustomerEdgeQueryHandler(ILoggerFactory loggerFactory, NpgsqlConnection dbConnection, IMapper mapper) : base(loggerFactory, dbConnection)
    {
        _mapper = mapper;
    }

    public override (List<M> models, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)
        MappingConfiguration(List<M> models, SqlStructure sqlStructure, object[] map, List<Type> types, List<string> aliases)
    {
        var customerCustomerEdges = models.OfType<CustomerCustomerEdge>().ToList();
        var totalCount = 0;
        var pageRecords = 0;
        var customerCustomerEdge = new CustomerCustomerEdge();

        for (int i = 0; i < map.Length; i++)
        {
            if (map[i] is TotalPageRecords)
            {
                pageRecords = (map[i] as TotalPageRecords).PageRecords;
            }
            else if (map[i] is TotalRecordCount)
            {
                totalCount = (map[i] as TotalRecordCount).RecordCount;
            }
            else
            {
                customerCustomerEdge = (CustomerCustomerEdge)_mapper.MapDynamicToModel(
                    customerCustomerEdge,
                    types[i],
                    map[i],
                    aliases[i]
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
}