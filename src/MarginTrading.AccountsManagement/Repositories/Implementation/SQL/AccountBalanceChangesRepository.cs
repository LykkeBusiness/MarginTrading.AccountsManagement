﻿using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Dapper;
using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Settings;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    public class AccountBalanceChangesRepository : IAccountBalanceChangesRepository
    {
        private const string TableName = "AccountBalanceChanges";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 "[Oid] [bigint] NOT NULL IDENTITY (1,1) PRIMARY KEY," +
                                                 "[AccountId] [nvarchar] (64) NOT NULL," +
                                                 "[ChangeTimestamp] [datetime] NOT NULL," +
                                                 "[Id] [nvarchar] (64) NOT NULL, " +
                                                 "[ClientId] [nvarchar] (64) NOT NULL, " +
                                                 "[ChangeAmount] [float] NOT NULL, " +
                                                 "[Balance] [float] NOT NULL, " +
                                                 "[WithdrawTransferLimit] [float] NOT NULL, " +
                                                 "[Comment] [nvarchar] (MAX) NOT NULL, " +
                                                 "[Type] [nvarchar] (64) NOT NULL, " +
                                                 "[EventSourceId] [nvarchar] (64) NOT NULL, " +
                                                 "[LegalEntity] [nvarchar] (64) NOT NULL, " +
                                                 "[AuditLog] [nvarchar] (MAX) NOT NULL " +
                                                 ");";
        
        private static Type DataType => typeof(IAccountBalanceChange);
        private static readonly string GetColumns = string.Join(",", DataType.GetProperties().Select(x => x.Name));
        private static readonly string GetFields = string.Join(",", DataType.GetProperties().Select(x => "@" + x.Name));
        private static readonly string GetUpdateClause = string.Join(",",
            DataType.GetProperties().Select(x => "[" + x.Name + "]=@" + x.Name));

        private readonly IConvertService _convertService;
        private readonly AccountManagementSettings _settings;
        private readonly ILog _log;
        
        public AccountBalanceChangesRepository(IConvertService convertService, AccountManagementSettings settings, ILog log)
        {
            _convertService = convertService;
            _log = log;
            _settings = settings;
            
            using (var conn = new SqlConnection(_settings.Db.SqlConnectionString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(AccountBalanceChangesRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }
        
        public async Task<List<AccountBalanceChange>> GetAsync(string[] accountIds, DateTime? @from, DateTime? to)
        {
            var whereClause = "WHERE " + (accountIds.Any() ? $"AccountId IN ('{string.Join("','", accountIds)}')" : "")
                                       + (from != null ? " AND ChangeTimestamp > @from" : "")
                                       + (to != null ? " AND ChangeTimestamp < @to" : "");
            
            using (var conn = new SqlConnection(_settings.Db.SqlConnectionString))
            {
                var data = await conn.QueryAsync<AccountBalanceChangeEntity>(
                    $"SELECT * FROM {TableName} {whereClause}", new { from, to });

                return data.Select(x => _convertService.Convert<AccountBalanceChange>(x)).ToList();
            }
        }

        public async Task AddAsync(AccountBalanceChange change)
        {
            var entity = _convertService.Convert<AccountBalanceChangeEntity>(change);
            
            using (var conn = new SqlConnection(_settings.Db.SqlConnectionString))
            {
                try
                {
                    try
                    {
                        await conn.ExecuteAsync(
                            $"insert into {TableName} ({GetColumns}) values ({GetFields})", entity);
                    }
                    catch (SqlException)
                    {
                        await conn.ExecuteAsync(
                            $"update {TableName} set {GetUpdateClause} where Id=@Id", entity); 
                    }
                }
                catch (Exception ex)
                {
                    var msg = $"Error {ex.Message} \n" +
                              "Entity <AccountBalanceChangeEntity>: \n" +
                              entity.ToJson();
                    await _log.WriteWarningAsync(nameof(AccountBalanceChangesRepository), nameof(AddAsync), null, msg);
                    throw new Exception(msg);
                }
            }
        }
    }
}