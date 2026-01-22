using System.Data;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Extensions.Logging;
using Npgsql;
using CoffeeBeanery.CQRS;
using System.Collections.Generic;
using CoffeeBeanery.GraphQL.Core.Sql;

namespace CoffeeBeanery.Service
{
    public abstract class ProcessQuery<M>
        where M : class
    {
        protected readonly ILogger _logger;
        protected readonly NpgsqlConnection _dbConnection;
        private List<M> _models;

        protected ProcessQuery(ILoggerFactory loggerFactory, NpgsqlConnection dbConnection)
        {
            _logger = loggerFactory.CreateLogger(typeof(ProcessQuery<M>).FullName);
            _dbConnection = dbConnection;
            _models = new List<M>();
        }

        public async Task<(List<M> models, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)> 
            ExecuteAsync(ProcessQueryParameters parameters, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(parameters.SqlStructure.SqlQuery))
                return (new List<M>(), null, null, null, null);
            // Ensure SplitOnDapper is not null, if so return empty result
            if (parameters.SplitOnDapper == null || parameters.SplitOnDapper.Count == 0)
            {
                return (new List<M>(), 0, 0, 0, 0);
            }

            // Create dynamic splitOnTypes and splitOn for Dapper query
            var splitOnTypes = parameters.SplitOnDapper.Values.Distinct().ToList();
            var splitOn = parameters.SplitOnDapper.Values
                .Select(a => nameof(a)).ToList();

            // Handle pagination and total count fields in the query
            if (parameters.Pagination.TotalRecordCount.RecordCount > 0 && parameters.Pagination.TotalPageRecords.PageRecords > 0)
            {
                splitOnTypes.Add(typeof(TotalPageRecords)); // Add the pagination type
                splitOnTypes.Add(typeof(TotalRecordCount)); // Add the record count type
                splitOn.Insert(0, "RowNumber"); // Ensure RowNumber is the first column in SplitOn
            }

            // Open connection and start a database transaction
            await using var connection = _dbConnection;
            await connection.OpenAsync(cancellationToken);
            var dbTransaction = await connection.BeginTransactionAsync(cancellationToken);

            try
            {
                // Execute the query using Dapper
                var result = await connection.QueryAsync<(int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords)>(
                    $"{parameters.SqlStructure.SqlUpsert};{parameters.SqlStructure.SqlQuery}",
                    splitOnTypes.ToArray(),
                    map =>
                    {
                        // Map the Dapper results to models and return pagination info
                        var mappingResult = MapToModel(_models, map);
                        _models = mappingResult.models;
                        return (mappingResult.startCursor, mappingResult.endCursor, mappingResult.totalCount, mappingResult.totalPageRecords);
                    },
                    splitOn: string.Join(",", splitOn),
                    transaction: dbTransaction,
                    commandType: CommandType.Text
                );

                // Commit the transaction
                await dbTransaction.CommitAsync(cancellationToken);

                // Return early if no results are found
                if (result == null || !result.Any())
                {
                    return (new List<M>(), 0, 0, 0, 0);
                }

                // Return the final models and pagination information
                return (_models,
                        parameters.Pagination.StartCursor > 0 ? parameters.Pagination.StartCursor : result.Select(s => s.startCursor).FirstOrDefault(),
                        parameters.Pagination.EndCursor > 0 ? parameters.Pagination.EndCursor : result.Select(s => s.endCursor).FirstOrDefault(),
                        result.Select(s => s.totalCount).FirstOrDefault(),
                        result.Select(s => s.totalPageRecords).FirstOrDefault());
            }
            catch (Exception ex)
            {
                // Rollback transaction on error
                await dbTransaction.RollbackAsync(cancellationToken);
                _logger.LogError(ex, "Error processing SQL query");
            }

            // Return default result in case of failure
            return (new List<M>(), 0, 0, 0, 0);
        }

        /// <summary>
        /// A virtual method for mapping Dapper results to the models dynamically.
        /// </summary>
        protected abstract (List<M> models, int? startCursor, int? endCursor, int? totalCount, int? totalPageRecords) MapToModel(
            List<M> models, object[] rowParts);
    }
}
