using System.Collections.Generic;
using CoffeeBeanery.Service;
using Domain.Model;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Domain.Shared.Query
{
    public class CustomerCustomerEdgeQueryHandler : ProcessQuery<CustomerCustomerEdge>
    {
        public CustomerCustomerEdgeQueryHandler(
            ILoggerFactory loggerFactory,
            NpgsqlConnection dbConnection)
            : base(loggerFactory, dbConnection)
        {
        }

        protected override (List<CustomerCustomerEdge> models, int? startCursor, int? endCursor,
            int? totalCount, int? totalPageRecords) MapToModel(
                List<CustomerCustomerEdge> models, object[] rowParts)
        {
            // Single‑entity case — rowParts contains one object you can cast
            if (rowParts.Length > 0 && rowParts[0] is CustomerCustomerEdge e)
            {
                models.Add(e);
            }

            return (models, null, null, null, null);
        }
    }
}