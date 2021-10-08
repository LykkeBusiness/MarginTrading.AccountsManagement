// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Common;
using Common.Log;
using Dapper;
using Lykke.Logs.MsSql.Extensions;
using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    internal class AccountsRepository : IAccountsRepository
    {
        private enum ClientSearchBy
        {
            Id,
            Account
        }
        
        #region SQL

        private const string CreateConstraintScript = @"
if object_id('dbo.[FK_MarginTradingAccounts_MarginTradingClients]', 'F') is null 
begin   
	alter table [dbo].[MarginTradingAccounts]  with check add constraint [FK_MarginTradingAccounts_MarginTradingClients] foreign key ([ClientId])
	references [dbo].[MarginTradingClients] ([Id])
end;
";
        private const string AccountsTableName = "MarginTradingAccounts";
        private const string CreateAccountsTableScript = "CREATE TABLE [{0}](" +
                                                 "[Id] [nvarchar] (64) NOT NULL PRIMARY KEY, " +
                                                 "[ClientId] [nvarchar] (64) NOT NULL, " +
                                                 "[BaseAssetId] [nvarchar] (64) NOT NULL, " +
                                                 "[Balance] decimal (24, 12) NOT NULL, " +
                                                 "[WithdrawTransferLimit] decimal (24, 12) NOT NULL, " +
                                                 "[LegalEntity] [nvarchar] (64) NOT NULL, " +
                                                 "[IsDisabled] [bit] NOT NULL, " +
                                                 "[IsWithdrawalDisabled] [bit] NOT NULL, " +
                                                 "[IsDeleted] [bit] NOT NULL, " +
                                                 "[ModificationTimestamp] [DateTime] NOT NULL, " +
                                                 "[TemporaryCapital] [nvarchar] (MAX) NOT NULL, " +
                                                 "[AdditionalInfo] [nvarchar] (MAX) NOT NULL, " +
                                                 "[LastExecutedOperations] [nvarchar] (MAX) NOT NULL, " +
                                                 "[AccountName] [nvarchar] (255), " +
                                                 "INDEX IX_{0} (ClientId, IsDeleted)" +
                                                 ");";


        private const string ClientsTableName = "MarginTradingClients";
        private const string CreateClientsTableScript = "CREATE TABLE [{0}](" +
                                                 "[Id] [nvarchar] (64) NOT NULL PRIMARY KEY, " +
                                                 "[TradingConditionId] [nvarchar] (64) NOT NULL" +
                                                 ");";

        private const string DeleteProcName = "DeleteAccountData";
        private readonly string DeleteProcCreateScript = $@"CREATE OR ALTER PROCEDURE [dbo].[{DeleteProcName}] (
        @AccountId NVARCHAR(128)
        )
        AS
            BEGIN
                SET NOCOUNT ON;
                BEGIN TRANSACTION
          
                DELETE [dbo].[AccountHistory] WHERE AccountId = @AccountId;
                DELETE [dbo].[OrdersHistory] WHERE AccountId = @AccountId;
                DELETE [dbo].[PositionsHistory] WHERE AccountId = @AccountId;
                DELETE [dbo].[Trades] WHERE AccountId = @AccountId;
                DELETE [dbo].[Deals] WHERE AccountId = @AccountId;
                DELETE [dbo].[Activities] WHERE AccountId = @AccountId;
          
                COMMIT TRANSACTION
            END;";

        #endregion SQL
        
        private static Type AccountDataType => typeof(IAccount);
        private static readonly PropertyInfo[] AccountProperties = AccountDataType.GetProperties()
            .Where(p => p.Name != nameof(IAccount.TradingConditionId)).ToArray();

        private static readonly string GetAccountColumns = string.Join(",", AccountProperties.Select(x => x.Name));
        private static readonly string GetAccountFields = string.Join(",", AccountProperties.Select(x => "@" + x.Name));
        private static readonly string GetAccountUpdateClause = string.Join(",", AccountProperties.Select(x => "[" + x.Name + "]=@" + x.Name));

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
            
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                try
                {
                    conn.CreateTableIfDoesntExists(CreateClientsTableScript, ClientsTableName);
                    conn.CreateTableIfDoesntExists(CreateAccountsTableScript, AccountsTableName);
                    conn.Execute(CreateConstraintScript);
                    conn.Execute(DeleteProcCreateScript);
                }
                catch (Exception ex)
                {
                    _log?.WriteErrorAsync(nameof(AccountsRepository), "Initialization", null, ex);
                    throw;
                }
            }
        }

        public async Task AddAsync(IAccount account)
        {
            await InsertClientIfNotExists(ClientEntity.From(account));
            await using var conn = new SqlConnection(_settings.Db.ConnectionString);
            await conn.ExecuteAsync($"insert into {AccountsTableName} ({GetAccountColumns}) values ({GetAccountFields})", Convert(account));
        }
        
        public async Task<IReadOnlyList<IAccount>> GetAllAsync(string clientId = null, string search = null,
            bool showDeleted = false)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = "%" + search + "%";
            }
            
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                var whereClause = "WHERE 1=1"
                                  + (string.IsNullOrWhiteSpace(clientId) ? "" : " AND a.ClientId = @clientId")
                                  + (string.IsNullOrWhiteSpace(search) ? "" : " AND (a.AccountName LIKE @search OR a.Id LIKE @search)")
                                  + (showDeleted ? "" : " AND a.IsDeleted = 0");
                var accounts = await conn.QueryAsync<AccountEntity>(
                    $"SELECT a.*, c.TradingConditionId FROM {AccountsTableName} a join {ClientsTableName} c on c.Id = a.ClientId {whereClause}", 
                    new { clientId, search });
                
                return accounts.ToList();
            }
        }

        public async Task<PaginatedResponse<IAccount>> GetByPagesAsync(string search = null, bool showDeleted = false,
            int? skip = null, int? take = null, bool isAscendingOrder = true)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = "%" + search + "%";
            }
            
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                var whereClause = "WHERE a.ClientId=c.Id"
                                  + (string.IsNullOrWhiteSpace(search) ? "" : " AND (a.AccountName LIKE @search OR a.Id LIKE @search)")
                                  + (showDeleted ? "" : " AND a.IsDeleted = 0");

                var paginationClause = $" ORDER BY [Id] {(isAscendingOrder ? "ASC" : "DESC")} OFFSET {skip ?? 0} ROWS FETCH NEXT {PaginationHelper.GetTake(take)} ROWS ONLY";
                var gridReader = await conn.QueryMultipleAsync(
                    $"SELECT a.*, c.TradingConditionId FROM {AccountsTableName} a, {ClientsTableName} c {whereClause} {paginationClause}; " +
                    $"SELECT COUNT(*) FROM {AccountsTableName} a, {ClientsTableName} c {whereClause}",
                    new {search});
                var accounts = (await gridReader.ReadAsync<AccountEntity>()).ToList();
                var totalCount = await gridReader.ReadSingleAsync<int>();

                return new PaginatedResponse<IAccount>(
                    accounts, 
                    skip ?? 0, 
                    accounts.Count, 
                    totalCount
                );
            }
        }

        public async Task<IAccount> GetAsync(string accountId)
        {
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                var whereClause = "WHERE 1=1 "
                                  + (string.IsNullOrWhiteSpace(accountId) ? "" : " AND a.Id = @accountId");
                var accounts = await conn.QueryAsync<AccountEntity>(
                    $"SELECT a.*, c.TradingConditionId FROM {AccountsTableName} a join {ClientsTableName} c on a.ClientId=c.Id {whereClause}", 
                    new { accountId });
                
                return accounts.FirstOrDefault();
            }
        }

        public async Task<IAccount> UpdateBalanceAsync(string operationId, string accountId,
            decimal amountDelta, bool changeLimit)
        {
            return await GetAccountAndUpdate(accountId, account =>
            {
                if (TryUpdateOperationsList(operationId, account))
                {
                    account.Balance += amountDelta;

                    if (changeLimit)
                        account.WithdrawTransferLimit += amountDelta;
                }
            });
        }

        public async Task<IAccount> UpdateAccountAsync(string accountId,  bool? isDisabled,
            bool? isWithdrawalDisabled)
        {
            return await GetAccountAndUpdate(accountId, a =>
            {
                if (isDisabled.HasValue)
                    a.IsDisabled = isDisabled.Value;

                if (isWithdrawalDisabled.HasValue)
                    a.IsWithdrawalDisabled = isWithdrawalDisabled.Value;
            });
        }

        public Task<IAccount> UpdateAdditionalInfo(string accountId, Action<AccountAdditionalInfo> mutate)
        {
            return GetAccountAndUpdate(accountId, a =>
            {
                var additionalInfo = ((IAccount) a).AdditionalInfo;
                if (additionalInfo == null)
                {
                    throw new InvalidOperationException($"{nameof(additionalInfo)} is null, this should not happen");
                }

                mutate(additionalInfo);
                a.AdditionalInfo = additionalInfo.ToJson(true);
            });
        }

        public async Task<IAccount> DeleteAsync(string accountId)
        {
            return await GetAccountAndUpdate(accountId, a =>
            {
                a.IsDeleted = true;
            });
        }

        public async Task<IAccount> UpdateAccountTemporaryCapitalAsync(string accountId,
            Func<string, List<TemporaryCapital>, TemporaryCapital, bool, List<TemporaryCapital>> handler,
            TemporaryCapital temporaryCapital, bool isAdd)
        {
            return await GetAccountAndUpdate(accountId, a =>
            {
                a.TemporaryCapital = handler(
                    accountId,
                    ((IAccount) a).TemporaryCapital,
                    temporaryCapital,
                    isAdd
                ).ToJson();
            });
        }

        public async Task<IAccount> RollbackTemporaryCapitalRevokeAsync(string accountId, 
            List<TemporaryCapital> revokedTemporaryCapital)
        {
            return await GetAccountAndUpdate(accountId, a =>
            {
                var result = ((IAccount) a).TemporaryCapital;

                result.AddRange(revokedTemporaryCapital.Where(x => result.All(r => r.Id != x.Id)));

                a.TemporaryCapital = result.ToJson();
            });
        }

        public async Task EraseAsync(string accountId)
        {
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                await conn.ExecuteAsync(
                    DeleteProcName,
                    new
                    {
                        AccountId = accountId,
                    },
                    commandType: CommandType.StoredProcedure,
                    commandTimeout: _settings.Db.LongRunningSqlTimeoutSec);
            }
        }

        #region Client

        private async Task InsertClientIfNotExists(ClientEntity client)
        {
            var sql = $@"
begin
   if not exists (select 1 from {ClientsTableName} where Id = @{nameof(ClientEntity.Id)})
   begin
       insert into {ClientsTableName} (Id, TradingConditionId) values (@{nameof(ClientEntity.Id)}, @{nameof(ClientEntity.TradingConditionId)}) 
   end
end
";
            await using var conn = new SqlConnection(_settings.Db.ConnectionString);
            await conn.ExecuteAsync(sql, client);
        }

        public async Task<PaginatedResponse<IClient>> GetClientsByPagesAsync(string tradingConditionId, int skip, int take)
        {
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                var whereClause = $"WHERE exists (select 1 from MarginTradingAccounts a where a.ClientId = c.Id){(string.IsNullOrEmpty(tradingConditionId) ? "" : " and c.TradingConditionId = @tradingConditionId")}";
                var paginationClause = $" ORDER BY [Id] ASC OFFSET {skip} ROWS FETCH NEXT {PaginationHelper.GetTake(take)} ROWS ONLY";
                var gridReader = await conn.QueryMultipleAsync($"SELECT * FROM {ClientsTableName} c {whereClause} {paginationClause}; " +
                                                               $"SELECT COUNT(*) FROM {ClientsTableName} c {whereClause}", new {tradingConditionId});
                var clients = (await gridReader.ReadAsync<ClientEntity>()).ToList();
                var totalCount = await gridReader.ReadSingleAsync<int>();

                return new PaginatedResponse<IClient>(
                    clients,
                    skip,
                    clients.Count,
                    totalCount
                );
            }
        }

        public async Task<PaginatedResponse<IClientSearchResult>> SearchByClientIdAsync(string clientId, int skip, int take)
        {
            using var conn = new SqlConnection(_settings.Db.ConnectionString);

            var gridReader = await conn.QueryMultipleAsync(
                BuildClientSearchSql(ClientSearchBy.Id, skip, take),
                new { query = clientId });
            
            var clients = (await gridReader.ReadAsync<ClientSearchResultEntity>()).ToList();
            var totalCount = await gridReader.ReadSingleAsync<int>();

            return new PaginatedResponse<IClientSearchResult>(
                clients,
                skip,
                clients.Count,
                totalCount
            );
        }

        public async Task<PaginatedResponse<IClientSearchResult>> SearchByAccountAsync(string IdOrName, int skip, int take)
        {
            using var conn = new SqlConnection(_settings.Db.ConnectionString);
            
            var gridReader = await conn.QueryMultipleAsync(
                BuildClientSearchSql(ClientSearchBy.Account, skip, take),
                new { query = IdOrName });
            
            var clients = (await gridReader.ReadAsync<ClientSearchResultEntity>()).ToList();
            var totalCount = await gridReader.ReadSingleAsync<int>();

            return new PaginatedResponse<IClientSearchResult>(
                clients,
                skip,
                clients.Count,
                totalCount
            );
        }

        private string BuildClientSearchSql(ClientSearchBy searchBy, int skip, int take)
        {
            var selectClause = @"
SELECT 
    ctc.ClientId, 
    ctc.TradingConditionId, 
    COALESCE(ctc.AccountNameCommaSeparatedList, ctc.AccountIdCommaSeparatedList) as AccountIdentityCommaSeparatedList
";

            var fromClause = @"
FROM (
         SELECT c.Id                                 as ClientId,
                c.TradingConditionId,
                STUFF((SELECT ',' + acc.AccountName
                       FROM MarginTradingAccounts acc
                       WHERE acc.ClientId = c.Id
                       FOR XML PATH ('')), 1, 1, '') AS AccountNameCommaSeparatedList,
                STUFF((SELECT ',' + acc.Id
                       FROM MarginTradingAccounts acc
                       WHERE acc.ClientId = c.Id
                       FOR XML PATH ('')),
                      1, 1, '')                      AS AccountIdCommaSeparatedList
         FROM MarginTradingClients c
                  INNER JOIN
              MarginTradingAccounts a on c.Id = a.ClientId
         GROUP by c.Id, c.TradingConditionId
     ) ctc
";
            var whereClause = "WHERE " + searchBy switch
            {
                ClientSearchBy.Id => 
                    $"ctc.ClientId LIKE CONCAT('%', @query, '%')",
                ClientSearchBy.Account => 
                    $"COALESCE(ctc.AccountNameCommaSeparatedList, ctc.AccountIdCommaSeparatedList) LIKE CONCAT('%', @query, '%')",
                _ => 
                    throw new ArgumentOutOfRangeException(nameof(searchBy), $"Unexpected type of search: {searchBy.ToString()}")
            };
            
            var paginationClause = $" ORDER BY ctc.TradingConditionId OFFSET {skip} ROWS FETCH NEXT {PaginationHelper.GetTake(take)} ROWS ONLY";

            return
                $"{selectClause} {fromClause} {whereClause} {paginationClause};" +
                $"SELECT COUNT(*) {fromClause} {whereClause}";
        }

        public async Task<IEnumerable<IClient>> GetClients(IEnumerable<string> clientIds)
        {
            await using var conn = new SqlConnection(_settings.Db.ConnectionString);
            var sqlParams = new { clientIds };
            return  await conn.QueryAsync<ClientEntity>($"select * from {ClientsTableName} where Id in @{nameof(sqlParams.clientIds)}", sqlParams);
        }

        public async Task<IEnumerable<IClient>> GetAllClients()
        {
            await using var conn = new SqlConnection(_settings.Db.ConnectionString);
            return await conn.QueryAsync<ClientEntity>($"select * from {ClientsTableName}");
        }

        public async Task<IClient> GetClient(string clientId, bool includeDeleted)
        {
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                var sqlParams = new { Id = clientId };

                return await conn.QuerySingleOrDefaultAsync<ClientEntity>(
                    $"SELECT * FROM {ClientsTableName} c where c.Id = @{nameof(sqlParams.Id)} " +
                    $"and exists (select 1 from {AccountsTableName} a where a.ClientId = c.Id" +
                    (includeDeleted ? "" : " and a.IsDeleted = 0") + ")",
                    sqlParams);
            }
        }
        
        public async Task UpdateClientTradingCondition(string clientId, string tradingConditionId)
        {
            var sqlParams = new { clientId, tradingConditionId };

            await using var conn = new SqlConnection(_settings.Db.ConnectionString);

            var affectedRows = await conn.ExecuteAsync($"update {ClientsTableName} set TradingConditionId = @{nameof(sqlParams.tradingConditionId)} " +
                                                       $"where Id = @{nameof(sqlParams.clientId)}",
                sqlParams);

            if (affectedRows != 1)
            {
                throw new InvalidOperationException($"Unexpected affected rows count {affectedRows} during {nameof(UpdateClientTradingCondition)}. " +
                                                    $"Sql params: {sqlParams.ToJson()}");
            }
        }

        #endregion

        #region Private Methods

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

        private async Task<IAccount> GetAccountAndUpdate(string accountId, Action<AccountEntity> handler)
        {
            using (var conn = new SqlConnection(_settings.Db.ConnectionString))
            {
                if (conn.State == ConnectionState.Closed)
                    await conn.OpenAsync();

                //Balance changing operation needs maximum level of isolation
                var transaction = conn.BeginTransaction(System.Data.IsolationLevel.Serializable);

                try
                {
                    var account = await conn.QuerySingleOrDefaultAsync<AccountEntity>(
                        $"SELECT a.*, c.TradingConditionId FROM {AccountsTableName} a WITH (UPDLOCK) join {ClientsTableName} c on c.Id=a.ClientId WHERE a.Id = @accountId", new {accountId}, transaction);

                    if (account == null)
                    {
                        throw new ValidationException( $"Account with ID {accountId} does not exist");
                    }

                    if (account.IsDeleted)
                    {
                        throw new ValidationException($"Account with ID {accountId} is deleted");
                    }

                    var tradingConditionBeforeUpdate = account.TradingConditionId;
                    handler(account);

                    if (account.TradingConditionId != tradingConditionBeforeUpdate)
                    {
                        throw new InvalidOperationException($"Update of {account.TradingConditionId} is not allowed on per account level. " +
                                                            $"Use Update for {ClientsTableName} table");
                    }

                    account.ModificationTimestamp = _systemClock.UtcNow.UtcDateTime;

                    await conn.ExecuteAsync(
                        $"update {AccountsTableName} set {GetAccountUpdateClause} where Id=@Id", account, transaction);

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

        private AccountEntity Convert(IAccount account)
        {
            if (account.AdditionalInfo == null)
            {
                throw new ArgumentException($"AdditionalInfo is null for {account.Id}", nameof(account.AdditionalInfo));
            }

            return new AccountEntity
            {
                Id = account.Id,
                ClientId = account.ClientId,
                TradingConditionId = account.TradingConditionId,
                BaseAssetId = account.BaseAssetId,
                Balance = account.Balance,
                WithdrawTransferLimit = account.WithdrawTransferLimit,
                LegalEntity = account.LegalEntity,
                IsDisabled = account.IsDisabled,
                IsWithdrawalDisabled = account.IsWithdrawalDisabled,
                ModificationTimestamp = account.ModificationTimestamp,
                TemporaryCapital = account.TemporaryCapital.ToJson(),
                LastExecutedOperations = account.LastExecutedOperations.ToJson(),
                AccountName = account.AccountName,
                AdditionalInfo = account.AdditionalInfo.ToJson(true)
            };
        }
        
        #endregion
    }
}