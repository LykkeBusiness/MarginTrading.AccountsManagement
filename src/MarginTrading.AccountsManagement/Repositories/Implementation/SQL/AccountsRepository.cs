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
using Dapper;
using Lykke.Snow.Common;
using Lykke.Snow.Common.Model;
using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.AccountsManagement.Dal.Common;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using Microsoft.Extensions.Internal;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    public class AccountsRepository : SqlRepositoryBase, IAccountsRepository
    
    {
        private const string AccountsTableName = "MarginTradingAccounts";
        private const string ClientsTableName = "MarginTradingClients";
        private const string DeleteProcName = "DeleteAccountData";

        private static Type AccountDataType => typeof(IAccount);

        private static readonly PropertyInfo[] AccountProperties = AccountDataType
            .GetProperties()
            .Where(p => p.Name != nameof(IAccount.TradingConditionId) && p.Name != nameof(IAccount.UserId))
            .ToArray();

        private static readonly string GetAccountColumns = string.Join(",", AccountProperties.Select(x => x.Name));
        private static readonly string GetAccountFields = string.Join(",", AccountProperties.Select(x => "@" + x.Name));
        private static readonly string GetAccountUpdateClause = string.Join(",", AccountProperties.Select(x => "[" + x.Name + "]=@" + x.Name));

        private readonly StoredProcedure _searchClients = new StoredProcedure("searchClients", "AccountsManagement", "dbo", null);
        private readonly StoredProcedure _getAllClients = new StoredProcedure("getAllClients", "AccountsManagement", "dbo", null);
        private readonly IConvertService _convertService;
        private readonly ISystemClock _systemClock;
        private readonly int _longRunningSqlTimeoutSec;
        private readonly ILogger _logger;
        private const int MaxOperationsCount = 200;

        public AccountsRepository(string connectionString,
            IConvertService convertService,
            ISystemClock systemClock,
            int longRunningSqlTimeoutSec,
            ILogger<AccountsRepository> logger) : base(connectionString, logger)
        {
            _convertService = convertService;
            _systemClock = systemClock;
            _longRunningSqlTimeoutSec = longRunningSqlTimeoutSec;
            _logger = logger;
        }

        public void Initialize()
        {
            ConnectionString.InitializeSqlObject("dbo.MarginTradingClients.sql", _logger);
            ConnectionString.InitializeSqlObject("dbo.MarginTradingAccounts.sql", _logger);
            ExecCreateOrAlter("dbo.searchClients.sql");
            ExecCreateOrAlter("dbo.getAllClients.sql");
            ExecCreateOrAlter("dbo.DeleteAccountData.sql");
        }

        public async Task AddAsync(IAccount account)
        {
            await InsertClientIfNotExists(ClientEntity.From(account));
            await using var conn = new SqlConnection(ConnectionString);
            await conn.ExecuteAsync($"insert into {AccountsTableName} ({GetAccountColumns}) values ({GetAccountFields})", Convert(account));
        }
        
        public async Task<IReadOnlyList<IAccountSuggested>> GetSuggestedListAsync(string query, int limit)
        {
            await using var conn = new SqlConnection(ConnectionString);
            var sql = $"SELECT TOP {limit} Id, AccountName FROM {AccountsTableName} WHERE Id LIKE @query OR AccountName LIKE @query ORDER BY Id";
            return (await conn.QueryAsync<AccountSuggestedEntity>(sql, new { query = $"{query}%" })).ToList();
        }
        
        public async Task<IReadOnlyList<IAccount>> GetAllAsync(string clientId = null, string search = null,
            bool showDeleted = false)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = "%" + search + "%";
            }

            await using var conn = new SqlConnection(ConnectionString);
            var whereClause = "WHERE 1=1"
                              + (string.IsNullOrWhiteSpace(clientId) ? "" : " AND a.ClientId = @clientId")
                              + (string.IsNullOrWhiteSpace(search) ? "" : " AND (a.AccountName LIKE @search OR a.Id LIKE @search)")
                              + (showDeleted ? "" : " AND a.IsDeleted = 0");
            var accounts = await conn.QueryAsync<AccountEntity>(
                $"SELECT a.*, c.TradingConditionId, c.UserId FROM {AccountsTableName} a join {ClientsTableName} c on c.Id = a.ClientId {whereClause}", 
                new { clientId, search });
                
            return accounts.ToList();
        }

        public async Task<PaginatedResponse<IAccount>> GetByPagesAsync(string search = null, bool showDeleted = false,
            int? skip = null, int? take = null, bool isAscendingOrder = true)
        {
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = "%" + search + "%";
            }

            await using var conn = new SqlConnection(ConnectionString);
            var whereClause = "WHERE a.ClientId=c.Id"
                              + (string.IsNullOrWhiteSpace(search) ? "" : " AND (a.AccountName LIKE @search OR a.Id LIKE @search OR c.Id LIKE @search OR c.UserId LIKE @search)")
                              + (showDeleted ? "" : " AND a.IsDeleted = 0");

            var paginationClause = $" ORDER BY [Id] {(isAscendingOrder ? "ASC" : "DESC")} OFFSET {skip ?? 0} ROWS FETCH NEXT {PaginationHelper.GetTake(take)} ROWS ONLY";
            var gridReader = await conn.QueryMultipleAsync(
                $"SELECT a.*, c.TradingConditionId, c.UserId FROM {AccountsTableName} a, {ClientsTableName} c {whereClause} {paginationClause}; " +
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

        public async Task<IAccount> GetAsync(string accountId)
        {
            return await GetAsync(accountId, true);
        }

        public async Task<IAccount> GetAsync(string accountId, bool includeDeleted)
        {
            await using var conn = new SqlConnection(ConnectionString);
            var whereClause = "WHERE 1=1 "
                              + (string.IsNullOrWhiteSpace(accountId) ? "" : " AND a.Id = @accountId")
                              + (includeDeleted ? "" : " and a.IsDeleted = 0");
            var accounts = await conn.QueryAsync<AccountEntity>(
                $"SELECT a.*, c.TradingConditionId, c.UserId FROM {AccountsTableName} a join {ClientsTableName} c on a.ClientId=c.Id {whereClause}", 
                new { accountId });
                
            return accounts.FirstOrDefault();
        }

        public async Task<(string baseAssetId, decimal? temporaryCapital)> GetBaseAssetIdAndTemporaryCapitalAsync(
            string accountId)
        {
            await using var conn = new SqlConnection(ConnectionString);
            var whereClause = "WHERE 1=1 " + (string.IsNullOrWhiteSpace(accountId) ? "" : " AND Id = @accountId");
            var account = await conn.QuerySingleOrDefaultAsync<BaseAssetIdAndTemporaryCapital>(
                $"SELECT TOP 1 {nameof(AccountEntity.BaseAssetId)}, {nameof(AccountEntity.TemporaryCapital)} FROM {AccountsTableName} {whereClause}", 
                new { accountId });

            var baseAssetId = account?.BaseAssetId;
            var temporaryCapital = JsonConvert.DeserializeObject<List<TemporaryCapital>>(account?.TemporaryCapital)
                ?.Sum(x => x.Amount) ?? default;
            return (baseAssetId, temporaryCapital);
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
            await using var conn = new SqlConnection(ConnectionString);
            await conn.ExecuteAsync(
                DeleteProcName,
                new
                {
                    AccountId = accountId,
                },
                commandType: CommandType.StoredProcedure,
                commandTimeout: _longRunningSqlTimeoutSec);
        }

        #region Client

        private async Task InsertClientIfNotExists(ClientEntity client)
        {
            var sql = $@"
begin
   if not exists (select 1 from {ClientsTableName} where Id = @{nameof(ClientEntity.Id)})
   begin
       insert into {ClientsTableName} (Id, TradingConditionId, UserId) values (@{nameof(ClientEntity.Id)}, @{nameof(ClientEntity.TradingConditionId)}, @{nameof(ClientEntity.UserId)}) 
   end
end
";
            await using var conn = new SqlConnection(ConnectionString);
            await conn.ExecuteAsync(sql, client);
        }

        public async Task<PaginatedResponse<IClient>> GetClientsByPagesAsync(string tradingConditionId, int skip, int take)
        {
            await using var conn = new SqlConnection(ConnectionString);
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

        public async Task<PaginatedResponse<IClientWithAccounts>> SearchByClientAsync(string query, int skip, int take)
        {
            var result = await base.GetAllAsync(_searchClients, skip, take, false,
                new[]
                {
                    new SqlParameter("@Query", query.AsSqlParameterValue())
                }, MapClientSearchResult);
            
            return new PaginatedResponse<IClientWithAccounts>(
                result.Items.ToList(),
                skip,
                result.Items.Count(),
                result.TotalItems
            );
        }

        public async Task<IEnumerable<IClient>> GetClients(IEnumerable<string> clientIds)
        {
            await using var conn = new SqlConnection(ConnectionString);
            var sqlParams = new { clientIds };
            return  await conn.QueryAsync<ClientEntity>($"select * from {ClientsTableName} where Id in @{nameof(sqlParams.clientIds)}", sqlParams);
        }

        public async Task<IEnumerable<IClient>> GetAllClients()
        {
            await using var conn = new SqlConnection(ConnectionString);
            return await conn.QueryAsync<ClientEntity>($"select * from {ClientsTableName}");
        }

        public async Task<IEnumerable<IClientWithAccounts>> GetAllClientsWithAccounts()
        {
            return await base.GetAllAsync(_getAllClients, null, MapClientSearchResult);
        }

        public async Task<IClient> GetClient(string clientId, bool includeDeleted)
        {
            await using var conn = new SqlConnection(ConnectionString);
            var sqlParams = new { Id = clientId };

            return await conn.QuerySingleOrDefaultAsync<ClientEntity>(
                $"SELECT * FROM {ClientsTableName} c where c.Id = @{nameof(sqlParams.Id)} " +
                $"and exists (select 1 from {AccountsTableName} a where a.ClientId = c.Id" +
                (includeDeleted ? "" : " and a.IsDeleted = 0") + ")",
                sqlParams);
        }
        
        public async Task UpdateClientTradingCondition(string clientId, string tradingConditionId)
        {
            var sqlParams = new { clientId, tradingConditionId };

            await using var conn = new SqlConnection(ConnectionString);

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
        
        private ClientWithAccountsEntity MapClientSearchResult(SqlDataReader reader)
        {
            return new ClientWithAccountsEntity
            {
                Id = reader[nameof(ClientWithAccountsEntity.Id)] as string,
                TradingConditionId = reader[nameof(ClientWithAccountsEntity.TradingConditionId)] as string,
                AccountIdentityCommaSeparatedList = reader[nameof(ClientWithAccountsEntity.AccountIdentityCommaSeparatedList)] as string,
                UserId = reader[nameof(ClientWithAccountsEntity.UserId)] as string
            };
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

        private async Task<IAccount> GetAccountAndUpdate(string accountId, Action<AccountEntity> handler)
        {
            await using var conn = new SqlConnection(ConnectionString);
            if (conn.State == ConnectionState.Closed)
                await conn.OpenAsync();

            //Balance changing operation needs maximum level of isolation
            var transaction = conn.BeginTransaction(IsolationLevel.Serializable);

            try
            {
                var account = await conn.QuerySingleOrDefaultAsync<AccountEntity>(
                    $"SELECT a.*, c.TradingConditionId, c.UserId FROM {AccountsTableName} a WITH (UPDLOCK) join {ClientsTableName} c on c.Id=a.ClientId WHERE a.Id = @accountId", new {accountId}, transaction);

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
                AdditionalInfo = account.AdditionalInfo.ToJson(true),
                UserId = account.UserId,
            };
        }
        
        #endregion
    }
}