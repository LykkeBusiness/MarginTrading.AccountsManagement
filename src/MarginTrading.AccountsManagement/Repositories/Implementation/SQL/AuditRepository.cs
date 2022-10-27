// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using MarginTrading.AccountsManagement.Dal.Common;
using MarginTrading.AccountsManagement.InternalModels;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    public class AuditRepository : IAuditRepository
    {
        private readonly ILogger _logger;
        private readonly string _connectionString;
        
        private static readonly string GetColumns = string.Join(",", typeof(DbSchema).GetProperties().Select(x => x.Name));

        public AuditRepository(string connectionString, ILogger<AuditRepository> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public void Initialize()
        {
            _connectionString.InitializeSqlObject("dbo.AuditTrail.sql", _logger);
        }

        public async Task InsertAsync(AuditModel model)
        {
            await using var conn = new SqlConnection(_connectionString);

            await conn.InsertAsync(DbSchema.FromDomain(model));
        }

        public async Task<PaginatedResponse<AuditModel>> GetAll(AuditLogsFilterDto filter, int? skip, int? take)
        {
            take = PaginationHelper.GetTake(take);
            
            var sqlFilter = filter.AddSqlLikeWildcards();

            var whereClause = "WHERE 1=1 "
                              + (!string.IsNullOrWhiteSpace(sqlFilter.UserName)
                                  ? " AND LOWER(UserName) like LOWER(@UserName)"
                                  : "")
                              + (!string.IsNullOrWhiteSpace(sqlFilter.CorrelationId)
                                  ? " AND CorrelationId=@CorrelationId"
                                  : "")
                              + (!string.IsNullOrWhiteSpace(sqlFilter.ReferenceId)
                                  ? " AND LOWER(DataReference) like LOWER(@ReferenceId)"
                                  : "")
                              + (sqlFilter.DataTypes.Any() ? " AND DataType IN @DataTypes" : "")
                              + (sqlFilter.ActionType != null ? " AND Type=@ActionType" : "")
                              + (sqlFilter.StartDateTime != null ? " AND Timestamp >= @StartDateTime" : "")
                              + (sqlFilter.EndDateTime != null ? " AND Timestamp <= @EndDateTime" : "");
            
            var paginationClause = $" ORDER BY [Timestamp] DESC OFFSET {skip ?? 0} ROWS FETCH NEXT {take} ROWS ONLY";

            await using var conn = new SqlConnection(_connectionString);

            var gridReader = await conn.QueryMultipleAsync(
                $"SELECT {GetColumns} FROM MarginTradingAccountsAuditTrail WITH (NOLOCK) {whereClause} {paginationClause}; SELECT COUNT(*) FROM MarginTradingAccountsAuditTrail {whereClause}", sqlFilter);

            var contents = (await gridReader.ReadAsync<DbSchema>())
                .Select(x => x.ToDomain())
                .ToList();
            
            var totalCount = await gridReader.ReadSingleAsync<int>();

            return new PaginatedResponse<AuditModel>(
                contents, 
                skip ?? 0, 
                contents.Count, 
                totalCount
            );
        }

        [Table("MarginTradingAccountsAuditTrail")]
        private sealed class DbSchema
        {
            [Key]
            public int Id { get; set; }
            
            public DateTime Timestamp { get; set; }
            
            public string CorrelationId { get; set; }
            
            public string UserName { get; set; }
            
            public string Type { get; set; }
            
            public string DataType { get; set; }
            
            public string DataReference { get; set; }
            
            public string DataDiff { get; set; }

            public AuditModel ToDomain()
            {
                return new AuditModel
                {
                    Id = Id,
                    Timestamp = Timestamp,
                    Type = Enum.Parse<AuditEventType>(Type),
                    CorrelationId = CorrelationId,
                    DataDiff = DataDiff,
                    DataReference = DataReference,
                    DataType = Enum.Parse<AuditDataType>(DataType),
                    UserName = UserName
                };
            }

            public static DbSchema FromDomain(AuditModel source)
            {
                return new DbSchema
                {
                    Timestamp = source.Timestamp,
                    CorrelationId = source.CorrelationId,
                    Type = source.Type.ToString(),
                    DataDiff = source.DataDiff,
                    DataReference = source.DataReference,
                    DataType = source.DataType.ToString(),
                    UserName = source.UserName,
                };
            }
        }
    }
}