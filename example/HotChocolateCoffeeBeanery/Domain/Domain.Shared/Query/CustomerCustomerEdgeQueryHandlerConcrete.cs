// CustomerCustomerEdgeQueryHandlerConcrete.cs
using System.Collections.Generic;
using CoffeeBeanery.Service;
using Microsoft.Extensions.Logging;
using Npgsql;
using Domain.Model;

namespace Domain.Shared.Query
{
    public class CustomerCustomerEdgeQueryHandlerConcrete : ProcessQuery<CustomerCustomerEdge>
    {
        public CustomerCustomerEdgeQueryHandlerConcrete(
            ILoggerFactory loggerFactory,
            NpgsqlConnection dbConnection)
            : base(loggerFactory, dbConnection)
        { }

        protected override (List<CustomerCustomerEdge> models, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords) MapToModel(
            List<CustomerCustomerEdge> models, object[] rowParts)
        {
            // Example mapping logic
            if (rowParts.Length > 0 && rowParts[0] is CustomerCustomerEdge edge)
            {
                models.Add(edge);
            }
            return (models, null, null, null, null);
        }
    }
}