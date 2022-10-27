using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Dapper.Contrib.Extensions;
using Lykke.Common.MsSql;
using MarginTrading.AccountsManagement.Dal.Common;
using MarginTrading.AccountsManagement.InternalModels;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    public class ComplexityWarningRepository : IComplexityWarningRepository
    {
        private readonly string _connectionString;
        private readonly ILogger _logger;

        public ComplexityWarningRepository(string connectionString, ILogger<ComplexityWarningRepository> logger)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public void Initialize()
        {
            _connectionString.InitializeSqlObject("dbo.ComplexityWarning.sql", _logger);
        }

        public async Task<ComplexityWarningState> GetOrCreate(string accountId, Func<ComplexityWarningState> factory)
        {
            await using var conn = new SqlConnection(_connectionString);

            var existed = await conn.GetAsync<DbSchema>(accountId);
            if (existed != null)
            {
                return existed.ToDomain();
            }

            var newItem = DbSchema.FromDomain(factory());
            
            try
            {
                await conn.InsertAsync(newItem);
            }
            catch (SqlException e) when (e.Number.IsDuplicateKeyViolation())
            {
                _logger.LogWarning(e,
                    "Optimistic concurrency control violated: Entity with id {AccountId} already exists,  use that value",
                    accountId);
            }

            return (await conn.GetAsync<DbSchema>(accountId)).ToDomain();
        }

        public async Task Save(ComplexityWarningState entity)
        {
            await using var conn = new SqlConnection(_connectionString);

            var propsToUpdate = typeof(DbSchema).GetProperties().Where(p => p.Name != nameof(DbSchema.RowVersion));
            var updateStatement =  string.Join(",", propsToUpdate.Select(x => "[" + x.Name + "]=@" + x.Name));

            //Dapper.Contrib does not support optimistic concurrency, that's why we have to write plain sql here instead of Update
            //see https://github.com/StackExchange/Dapper/issues/851
            var sql = @$"
update dbo.MarginTradingAccountsComplexityWarnings
set {updateStatement}
where AccountId = @{nameof(DbSchema.AccountId)} and RowVersion = @{nameof(DbSchema.RowVersion)}
";
            var dbEntity = DbSchema.FromDomain(entity);
            var rowsUpdated =  await conn.ExecuteAsync(sql, dbEntity);

            if (rowsUpdated != 1)
            {
                throw new InvalidOperationException($"Optimistic concurrency happened while updating product complexity state for account {entity.AccountId}");
            }
        }

        public async Task<IEnumerable<ComplexityWarningState>> GetExpired(DateTime timestamp)
        {
            await using var conn = new SqlConnection(_connectionString);

            var sqlParams = new {timestamp};
            var sql = $"select * from dbo.MarginTradingAccountsComplexityWarnings where SwitchedToFalseAt < @{nameof(sqlParams.timestamp)}";
            var dbEntities = await conn.QueryAsync<DbSchema>(sql, sqlParams);

            return dbEntities.Select(p => p.ToDomain());
        }

        [Table("MarginTradingAccountsComplexityWarnings")]
        private class DbSchema
        {
            [ExplicitKey]
            public string AccountId { get; set; }

            [Computed]
            public byte[] RowVersion { get; set; }

            public bool ShouldShowComplexityWarning { get; set; }

            public DateTime? SwitchedToFalseAt { get; set; }

            public string ConfirmedOrders { get; set; }

            public ComplexityWarningState ToDomain()
            {
                var confirmedOrders = JsonConvert
                    .DeserializeObject<List<ComplexityWarningState.OrderInfo>>(ConfirmedOrders)?
                    .ToDictionary(p => p.OrderId);

                return ComplexityWarningState.Restore(AccountId,
                    RowVersion,
                    confirmedOrders,
                    ShouldShowComplexityWarning,
                    SwitchedToFalseAt);
            }

            public static DbSchema FromDomain(ComplexityWarningState source)
            {
                return new DbSchema
                {
                    AccountId = source.AccountId,
                    RowVersion = source.RowVersion,
                    ShouldShowComplexityWarning = source.ShouldShowComplexityWarning,
                    SwitchedToFalseAt = source.SwitchedToFalseAt,
                    ConfirmedOrders = JsonConvert.SerializeObject(source.ConfirmedOrders.Values.ToList())
                };
            }
        }
    }
}
