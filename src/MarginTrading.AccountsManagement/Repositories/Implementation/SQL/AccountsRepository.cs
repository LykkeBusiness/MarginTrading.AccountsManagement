﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using System.Transactions;
using AutoMapper;
using AzureStorage;
using Common.Log;
using Dapper;
using Lykke.SettingsReader;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Repositories.AzureServices;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    internal class AccountsRepository : IAccountsRepository
    {
        private const string TableName = "MarginTradingAccounts";
        private const string CreateTableScript = "CREATE TABLE [{0}](" +
                                                 "[Id] [nvarchar] (64) NOT NULL PRIMARY KEY," +
                                                 "[ClientId] [nvarchar] (64) NOT NULL, " +
                                                 "[TradingConditionId] [nvarchar] (64) NOT NULL, " +
                                                 "[BaseAssetId] [nvarchar] (64) NOT NULL, " +
                                                 "[Balance] decimal (24, 12) NOT NULL, " +
                                                 "[WithdrawTransferLimit] decimal (24, 12) NOT NULL, " +
                                                 "[LegalEntity] [nvarchar] (64) NOT NULL, " +
                                                 "[IsDisabled] [bit] NOT NULL, " +
                                                 "[IsWithdrawalDisabled] [bit] NOT NULL, " +
                                                 "[ModificationTimestamp] [DateTime] NOT NULL," +
                                                 "[LastExecutedOperations] [nvarchar] (MAX) NOT NULL" +
                                                 ");";
        
        private static Type DataType => typeof(IAccount);
        private static readonly string GetColumns = string.Join(",", DataType.GetProperties().Select(x => x.Name));
        private static readonly string GetFields = string.Join(",", DataType.GetProperties().Select(x => "@" + x.Name));
        private static readonly string GetUpdateClause = string.Join(",",
            DataType.GetProperties().Select(x => "[" + x.Name + "]=@" + x.Name));

        private readonly IConvertService _convertService;
        private readonly ISystemClock _systemClock;
        private readonly AccountManagementSettings _settings;
        private readonly ILog _log;
        private const int MaxOperationsCount = 200;
        
        public AccountsRepository(IConvertService convertService, ISystemClock systemClock, 
            AccountManagementSettings settings, ILog log)
        {
            _convertService = convertService;
            _systemClock = systemClock;
            _log = log;
            _settings = settings;
            
            using (var conn = new SqlConnection(_settings.Db.SqlConnectionString))
            {
                try { conn.CreateTableIfDoesntExists(CreateTableScript, TableName); }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(AccountsRepository), "CreateTableIfDoesntExists", null, ex);
                    throw;
                }
            }
        }

        public async Task AddAsync(IAccount account)
        {
            using (var conn = new SqlConnection(_settings.Db.SqlConnectionString))
            {
                await conn.ExecuteAsync(
                    $"insert into {TableName} ({GetColumns}) values ({GetFields})", Convert(account));
            }
        }

        public async Task<IReadOnlyList<IAccount>> GetAllAsync(string clientId = null, string search = null)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = "%" + search + "%";
            }
            
            using (var conn = new SqlConnection(_settings.Db.SqlConnectionString))
            {
                var whereClause = "WHERE 1=1" +
                                  (string.IsNullOrWhiteSpace(clientId) ? "" : " AND ClientId = @clientId")
                    + (string.IsNullOrWhiteSpace(search) ? "" : " AND Id LIKE @search");
                var accounts = await conn.QueryAsync<AccountEntity>(
                    $"SELECT * FROM {TableName} {whereClause}", 
                    new { clientId, search });
                
                return accounts.ToList();
            }
        }

        public async Task<PaginatedResponse<IAccount>> GetByPagesAsync(string search = null, int? skip = null, int? take = null)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = "%" + search + "%";
            }
            
            using (var conn = new SqlConnection(_settings.Db.SqlConnectionString))
            {
                var whereClause = "WHERE 1=1"
                                  + (string.IsNullOrWhiteSpace(search) ? "" : " AND Id LIKE @search");

                var paginationClause = $" ORDER BY [Id] OFFSET {skip ?? 0} ROWS FETCH NEXT {PaginationHelper.GetTake(take)} ROWS ONLY";
                var gridReader = await conn.QueryMultipleAsync(
                    $"SELECT * FROM {TableName} {whereClause} {paginationClause}; SELECT COUNT(*) FROM {TableName} {whereClause}",
                    new {search});
                var accounts = (await gridReader.ReadAsync<AccountEntity>()).ToList();
                var totalCount = await gridReader.ReadSingleAsync<int>();

                return new PaginatedResponse<IAccount>(
                    contents: accounts, 
                    start: skip ?? 0, 
                    size: accounts.Count, 
                    totalSize: !take.HasValue ? accounts.Count : totalCount
                );
            }
        }

        public async Task<IAccount> GetAsync(string clientId, string accountId)
        {
            using (var conn = new SqlConnection(_settings.Db.SqlConnectionString))
            {
                var whereClause = "WHERE 1=1 "
                                  + (string.IsNullOrWhiteSpace(accountId) ? "" : " AND Id = @accountId")
                                  + (string.IsNullOrWhiteSpace(clientId) ? "" : " AND ClientId = @clientId");
                var accounts = await conn.QueryAsync<AccountEntity>(
                    $"SELECT * FROM {TableName} {whereClause}", 
                    new { clientId, accountId });
                
                return accounts.FirstOrDefault();
            }
        }

        public async Task<IAccount> GetAsync(string accountId)
        {
            return await GetAsync(null, accountId);
        }

        public async Task<IAccount> UpdateBalanceAsync(string operationId, string clientId, string accountId,
            decimal amountDelta, bool changeLimit)
        {
            return await GetAccountAndUpdate(accountId, account =>
            {
                if (TryUpdateOperationsList(operationId, account))
                {
                    account.Balance += amountDelta;

                    if (changeLimit)
                        account.WithdrawTransferLimit += amountDelta;
                    
                    account.ModificationTimestamp = _systemClock.UtcNow.UtcDateTime;
                }
            });
        }

        private bool TryUpdateOperationsList(string operationId, AccountEntity a)
        {
            var lastExecutedOperations = _convertService.Convert<List<string>>(a.LastExecutedOperations);
            
            if (lastExecutedOperations.Contains(operationId))
                return false;
            
            lastExecutedOperations.Add(operationId);
            if (lastExecutedOperations.Count > MaxOperationsCount)
            {
                lastExecutedOperations.RemoveAt(0);
            }

            a.LastExecutedOperations = _convertService.Convert<string>(lastExecutedOperations);
            
            return true;
        }

        public async Task<IAccount> UpdateTradingConditionIdAsync(string clientId, string accountId,
            string tradingConditionId)
        {
            return await GetAccountAndUpdate(accountId, account => { account.TradingConditionId = tradingConditionId; });
        }

        public async Task<IAccount> ChangeIsDisabledAsync(string clientId, string accountId, bool isDisabled)
        {
            return await GetAccountAndUpdate(accountId, account => { account.IsDisabled = isDisabled; });
        }

        public async Task<IAccount> ChangeIsWithdrawalDisabledAsync(string clientId, string accountId, bool isDisabled)
        {
            return await GetAccountAndUpdate(accountId, account => { account.IsWithdrawalDisabled = isDisabled; });
        }

        private async Task<IAccount> GetAccountAndUpdate(string accountId, Action<AccountEntity> handler)
        {
            using (var conn = new SqlConnection(_settings.Db.SqlConnectionString))
            {
                if (conn.State == ConnectionState.Closed)
                    await conn.OpenAsync();

                //Balance changing operation needs maximum level of isolation
                var transaction = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);

                try
                {
                    var account = await conn.QuerySingleOrDefaultAsync<AccountEntity>(
                        $"SELECT * FROM {TableName} WITH (UPDLOCK) WHERE Id = @accountId", new {accountId}, transaction);

                    if (account == null)
                    {
                        throw new ArgumentNullException(nameof(accountId), "Account does not exist");
                    }

                    handler(account);

                    await conn.ExecuteAsync(
                        $"update {TableName} set {GetUpdateClause} where Id=@Id", account, transaction);

                    transaction.Commit();

                    return account;
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }

        private Account Convert(AccountEntity entity)
        {
            return _convertService.Convert<AccountEntity, Account>(
                entity,
                o => o.ConfigureMap(MemberList.Destination).ForCtorParam(
                    "modificationTimestamp",
                    m => m.MapFrom(e => e.ModificationTimestamp)));
        }

        private AccountEntity Convert(IAccount account)
        {
            return _convertService.Convert<IAccount, AccountEntity>(account);
        }
    }
}