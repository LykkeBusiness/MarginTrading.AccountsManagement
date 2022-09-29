// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Dapper;
using Lykke.Logs.MsSql.Extensions;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    internal class OperationExecutionInfoRepository : IOperationExecutionInfoRepository
    {
        private const string TableName = "OperationExecutionInfo";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 "[Oid] [bigint] NOT NULL IDENTITY(1,1) PRIMARY KEY, " +
                                                 "[Id] [nvarchar] (128) NOT NULL, " +
                                                 "[LastModified] [datetime] NOT NULL, " +
                                                 "[OperationName] [nvarchar] (64) NULL, " +
                                                 "[Version] [nvarchar] (64) NULL, " +
                                                 "[Data] [nvarchar] (MAX) NOT NULL, " +
                                                 "CONSTRAINT [UX_OpExInfo_Id_Op] UNIQUE NONCLUSTERED ([Id], [OperationName])" +
                                                 ");";
        
        private static Type DataType => typeof(IOperationExecutionInfo<object>);
        private static readonly string GetColumns = string.Join(",", DataType.GetProperties().Select(x => x.Name));
        private static readonly string GetFields = string.Join(",", DataType.GetProperties().Select(x => "@" + x.Name));
        private static readonly string GetUpdateClause = string.Join(",", 
            DataType.GetProperties().Select(x => "[" + x.Name + "]=@" + x.Name));

        private readonly string _connectionString;
        private readonly ISystemClock _systemClock;
        private readonly ILogger _logger;

        public OperationExecutionInfoRepository(AccountManagementSettings settings, 
            ISystemClock systemClock,
            ILogger<OperationExecutionInfoRepository> logger)
        {
            _connectionString = settings.Db.ConnectionString;
            _systemClock = systemClock;
            _logger = logger;

            using var conn = new SqlConnection(_connectionString);
            try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create table {TableName}", TableName);
                throw;
            }
        }
        
        public async Task<IOperationExecutionInfo<TData>> GetOrAddAsync<TData>(
            string operationName, string operationId, Func<IOperationExecutionInfo<TData>> factory) where TData : class
        {
            try
            {
                await using var conn = new SqlConnection(_connectionString);
                var entity = Convert(factory(), _systemClock.UtcNow.UtcDateTime);

                // Here "update when matched" is made as a workaround to get the entity back within inserted
                // collection when it has already existed before
                var operationInfo = await conn.QueryFirstOrDefaultAsync<OperationExecutionInfoEntity>(
                    $@"MERGE {TableName} AS target 
USING (SELECT @Id, @OperationName) AS source (Id, OperationName)
ON (target.Id = source.Id AND target.OperationName = source.OperationName)
WHEN MATCHED THEN UPDATE SET target.Id = source.Id
WHEN NOT MATCHED THEN
    INSERT ({GetColumns}) VALUES ({GetFields})
OUTPUT inserted.*;", entity);
                    
                return Convert<TData>(operationInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get or add operation execution info {OperationName} {OperationId}", 
                    operationName, operationId);
                throw;
            }
        }

        public async Task<IOperationExecutionInfo<TData>> GetAsync<TData>(string operationName, string id) where TData : class
        {
            await using var conn = new SqlConnection(_connectionString);
            var operationInfo = await conn.QuerySingleOrDefaultAsync<OperationExecutionInfoEntity>(
                $"SELECT * FROM {TableName} WHERE Id = @id and OperationName=@operationName",
                new {id, operationName});

            return operationInfo == null ? null : Convert<TData>(operationInfo);
        }

        public async Task SaveAsync<TData>(IOperationExecutionInfo<TData> executionInfo) where TData : class
        {
            var entity = Convert(executionInfo, _systemClock.UtcNow.UtcDateTime);
            int affectedRows;

            await using (var conn = new SqlConnection(_connectionString))
            {
                try
                {
                    affectedRows = await conn.ExecuteAsync(
                        $"update {TableName} set {GetUpdateClause} where Id=@Id " +
                        "and OperationName=@OperationName " +
                        "and LastModified=@PrevLastModified",
                        entity);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to save operation execution info {OperationName} {OperationId}", 
                        executionInfo.OperationName, executionInfo.Id);
                    throw;
                }
            }

            if (affectedRows == 0)
            {
                var existingExecutionInfo = await GetAsync<TData>(executionInfo.OperationName, executionInfo.Id);

                if (existingExecutionInfo == null)
                {
                    throw new InvalidOperationException(
                        $"Execution info {executionInfo.OperationName}:{executionInfo.Id} does not exist");
                }

                throw new InvalidOperationException(
                    $"Optimistic Concurrency Violation Encountered. " +
                    $"Existing info: [{existingExecutionInfo.ToJson()}] " +
                    $"New info: [{executionInfo.ToJson()}]");
            }
        }

        public async Task DeleteAsync<TData>(IOperationExecutionInfo<TData> executionInfo) where TData : class
        {
            var entity = Convert(executionInfo, _systemClock.UtcNow.UtcDateTime);

            await using var conn = new SqlConnection(_connectionString);
            try
            {
                await conn.ExecuteAsync(
                    $"DELETE {TableName} where Id=@Id " +
                    "and OperationName=@OperationName ",
                    entity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete operation execution info {OperationName} {OperationId}", 
                    executionInfo.OperationName, executionInfo.Id);
                throw;
            }
        }

        private static OperationExecutionInfo<TData> Convert<TData>(OperationExecutionInfoEntity entity)
            where TData : class
        {
            return new OperationExecutionInfo<TData>(
                entity.OperationName,
                entity.Id,
                lastModified: entity.LastModified,
                data: entity.Data is string dataStr
                    ? JsonConvert.DeserializeObject<TData>(dataStr)
                    : ((JToken) entity.Data).ToObject<TData>());
        }

        private static OperationExecutionInfoEntity Convert<TData>(IOperationExecutionInfo<TData> model, DateTime now)
            where TData : class
        {
            return new OperationExecutionInfoEntity
            {
                Id = model.Id,
                OperationName = model.OperationName,
                Data = model.Data.ToJson(),
                PrevLastModified = model.LastModified,
                LastModified = now
            };
        }
    }
}