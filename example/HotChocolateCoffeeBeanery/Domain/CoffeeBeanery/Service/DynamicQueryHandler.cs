using Npgsql;

namespace CoffeeBeanery.Service
{
    public interface IDynamicQueryHandler
    {
        protected (List<object> models, int? startCursor, int? endCursor,
            int? totalCount, int? totalPageRecords) MapToModel(
                List<object> models, object[] rowParts);

        Task<(List<object> models, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>
            ExecuteAsync(ProcessQueryParameters parameters, CancellationToken ct);
    }
    
    public class DynamicQueryHandler : ProcessQuery<object>, IDynamicQueryHandler
    {
        public DynamicQueryHandler(
            ILoggerFactory loggerFactory,
            NpgsqlConnection dbConnection)
            : base(loggerFactory, dbConnection)
        {
        }

        /// <summary>
        /// Maps the raw data returned by Dapper into a list of objects, and handles pagination/count if applicable.
        /// </summary>
        protected override (List<object> models, int? startCursor, int? endCursor,
            int? totalCount, int? totalPageRecords) MapToModel(
                List<object> models, object[] rowParts)
        {
            // Add each row part to model list.
            // For better mapping, you can map it to a specific type if needed.
            foreach (var part in rowParts)
            {
                if (part != null)
                {
                    models.Add(part);
                }
            }

            // Handle pagination and count if returned by SQL query
            int? startCursor = null;
            int? endCursor = null;
            int? totalCount = null;
            int? totalPageRecords = null;

            // Check if pagination data is included in the query result (e.g. total count, total pages)
            // This assumes the first two parts are startCursor and endCursor
            if (rowParts.Length >= 4)
            {
                startCursor = rowParts[0] as int?;
                endCursor = rowParts[1] as int?;
                totalCount = rowParts[2] as int?;
                totalPageRecords = rowParts[3] as int?;
            }

            // Return models with pagination/count data
            return (models, startCursor, endCursor, totalCount, totalPageRecords);
        }

        public async Task<(List<object> models, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)> 
            ExecuteAsync(ProcessQueryParameters parameters, CancellationToken ct)
        {
            return await base.ExecuteAsync(parameters, ct);
        }

        (List<object> models, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords) IDynamicQueryHandler.MapToModel(List<object> models,
            object[] rowParts)
        {
            return MapToModel(models, rowParts);
        }
    }
}