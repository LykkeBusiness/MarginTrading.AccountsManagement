﻿using System;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Dapper;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Models;
using MarginTrading.AccountsManagement.BrokerBase;
using MarginTrading.AccountsManagement.BrokerBase.Services;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker.Repositories.SqlRepositories
{
    public class AccountHistoryRepository : IAccountHistoryRepository
    {
        private const string TableName = "AccountHistory";

        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 "[Oid] [bigint] NOT NULL IDENTITY (1,1) PRIMARY KEY," +
                                                 "[Id] [nvarchar] (64) NOT NULL UNIQUE, " +
                                                 "[AccountId] [nvarchar] (64) NOT NULL," +
                                                 "[ChangeTimestamp] [datetime] NOT NULL," +
                                                 "[ClientId] [nvarchar] (64) NOT NULL, " +
                                                 "[ChangeAmount] decimal (24, 12) NOT NULL, " +
                                                 "[Balance] decimal (24, 12) NOT NULL, " +
                                                 "[WithdrawTransferLimit] decimal (24, 12) NOT NULL, " +
                                                 "[Comment] [nvarchar] (MAX) NULL, " +
                                                 "[ReasonType] [nvarchar] (64) NULL, " +
                                                 "[EventSourceId] [nvarchar] (64) NULL, " +
                                                 "[LegalEntity] [nvarchar] (64) NULL, " +
                                                 "[AuditLog] [nvarchar] (MAX) NULL, " +
                                                 "[Instrument] [nvarchar] (64) NULL, " +
                                                 "[TradingDate] [datetime] NULL" +
                                                 ");";
        
        private static Type DataType => typeof(IAccountHistory);
        private static readonly string GetColumns = string.Join(",", DataType.GetProperties().Select(x => x.Name));
        private static readonly string GetFields = string.Join(",", DataType.GetProperties().Select(x => "@" + x.Name));
        private static readonly string GetUpdateClause = string.Join(",",
            DataType.GetProperties().Select(x => "[" + x.Name + "]=@" + x.Name));

        private readonly Settings _settings;
        private readonly ILog _log;
        private readonly IConvertService _convertService;

        public AccountHistoryRepository(Settings settings, ILog log, IConvertService convertService)
        {
            _log = log;
            _settings = settings;
            _convertService = convertService;
            
            using (var conn = new SqlConnection(_settings.Db.HistorySqlConnString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(AccountHistoryRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }

        public async Task InsertOrReplaceAsync(IAccountHistory obj)
        {
            var entity = _convertService.Convert<AccountHistoryEntity>(obj);
            
            using (var conn = new SqlConnection(_settings.Db.HistorySqlConnString))
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
                           "Entity <IAccountTransactionsReport>: \n" +
                           entity.ToJson();
                    _log?.WriteWarningAsync(nameof(AccountHistoryRepository), nameof(InsertOrReplaceAsync), null, msg);
                    throw new Exception(msg);
                }
            }
        }
    }
}