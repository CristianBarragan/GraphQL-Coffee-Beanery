using CoffeeBeanery.CQRS;
using CoffeeBeanery.GraphQL.Core.Sql;
using CoffeeBeanery.Service;
using Domain.Model;
using DataEntity = Database.Entity;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Domain.Shared.Query;

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

    public override (List<M>, int?, int?, int?, int?) MappingConfiguration(
        List<M> models,
        SqlStructure sqlStructure,
        object[] map)
    {
        Console.WriteLine("=== MAPPING CONFIGURATION START ===");

        var wrapper = new Wrapper { CustomerCustomerEdge = new List<CustomerCustomerEdge>() };

        int totalCount = 0;
        int pageRecords = 0;

        if (map == null || map.Length == 0)
        {
            Console.WriteLine("Dapper map is null or empty.");
            return (new List<M>(), 0, 0, 0, 0);
        }

        foreach (var item in map)
        {
            if (item == null)
            {
                Console.WriteLine("NULL ITEM FROM DAPPER");
                continue;
            }

            Console.WriteLine($"ITEM TYPE: {item.GetType().FullName}");

            if (item is TotalPageRecords tpr)
            {
                pageRecords = tpr.PageRecords;
                Console.WriteLine($"TotalPageRecords: {pageRecords}");
                continue;
            }

            if (item is TotalRecordCount trc)
            {
                totalCount = trc.RecordCount;
                Console.WriteLine($"TotalRecordCount: {totalCount}");
                continue;
            }

            try
            {
                // 🔥 Map whatever we can from the entity to models
                var wrapped = WrapEntityByName(item);
                wrapper = Merge(wrapper, wrapped);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception wrapping entity: {ex.Message}");
            }
        }

        Console.WriteLine("=== MAPPING CONFIGURATION END ===");
        return (new List<M> { (M)(object)wrapper },
            sqlStructure.Pagination?.StartCursor,
            sqlStructure.Pagination?.EndCursor,
            totalCount,
            pageRecords);
    }

    private Wrapper WrapEntityByName(object entity)
    {
        var wrapper = new Wrapper { CustomerCustomerEdge = new List<CustomerCustomerEdge>() };

        Console.WriteLine($"Wrapping entity type: {entity.GetType().Name}");

        if (entity is DataEntity.CustomerCustomerRelationship ccr)
        {
            Console.WriteLine("Mapping CustomerCustomerRelationship using property names...");

            // Map the relationship itself
            var relationshipModel = _mapper.MapToModel<CustomerCustomerRelationship>(ccr, "CustomerCustomerRelationship");

            // Create the edge and set keys from entity
            var edge = new CustomerCustomerEdge
            {
                CustomerCustomerRelationship = relationshipModel,
                CustomerCustomerRelationshipKey = ccr.CustomerCustomerRelationshipKey,
                InnerCustomerKey = ccr.InnerCustomerKey,
                OuterCustomerKey = ccr.OuterCustomerKey
            };

            // Map InnerCustomer and OuterCustomer objects if they exist
            if (HasProperty(entity, "InnerCustomer"))
                edge.InnerCustomer = _mapper.MapByAlias(typeof(Customer), GetPropertyValue(entity, "InnerCustomer"), "InnerCustomer") as Customer;

            if (HasProperty(entity, "OuterCustomer"))
                edge.OuterCustomer = _mapper.MapByAlias(typeof(Customer), GetPropertyValue(entity, "OuterCustomer"), "OuterCustomer") as Customer;

            wrapper.CustomerCustomerEdge.Add(edge);
            Console.WriteLine("Added CustomerCustomerEdge to wrapper with keys set.");
        }
        else
        {
            Console.WriteLine($"Unknown entity type, skipping wrap: {entity.GetType().Name}");
        }

        return wrapper;
    }

    // Helpers to check property by name
    private static bool HasProperty(object obj, string propName) =>
        obj.GetType().GetProperty(propName) != null;

    private static object GetPropertyValue(object obj, string propName) =>
        obj.GetType().GetProperty(propName)?.GetValue(obj);

    private Wrapper Merge(Wrapper current, Wrapper incoming)
    {
        if (incoming?.CustomerCustomerEdge == null) return current;

        current.CustomerCustomerEdge ??= new List<CustomerCustomerEdge>();

        foreach (var edge in incoming.CustomerCustomerEdge)
        {
            var existing = current.CustomerCustomerEdge.FirstOrDefault(x =>
                x.InnerCustomerKey == edge.InnerCustomerKey &&
                x.OuterCustomerKey == edge.OuterCustomerKey &&
                x.CustomerCustomerRelationshipKey == edge.CustomerCustomerRelationshipKey
            );

            if (existing == null)
            {
                Console.WriteLine("Adding new CustomerCustomerEdge to wrapper");
                current.CustomerCustomerEdge.Add(edge);
            }
            else
            {
                Console.WriteLine("Merging existing CustomerCustomerEdge");
                MergeEdge(existing, edge);
            }
        }

        return current;
    }

    private void MergeEdge(CustomerCustomerEdge current, CustomerCustomerEdge incoming)
    {
        current.Clause = incoming.Clause ?? current.Clause;
        current.LevelDepth = incoming.LevelDepth ?? current.LevelDepth;
        current.LevelDirection = incoming.LevelDirection ?? current.LevelDirection;
        current.GraphType = incoming.GraphType ?? current.GraphType;
        current.InnerCustomer = incoming.InnerCustomer ?? current.InnerCustomer;
        current.OuterCustomer = incoming.OuterCustomer ?? current.OuterCustomer;
        current.CustomerCustomerRelationship = incoming.CustomerCustomerRelationship ?? current.CustomerCustomerRelationship;
    }
}