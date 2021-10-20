// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Snow.Common.Model;
using Lykke.Snow.Mdm.Contracts.BrokerFeatures;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.AccountsManagement.Exceptions;
using MarginTrading.AccountsManagement.Extensions;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.ErrorCodes;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AssetService.Contracts;
using MarginTrading.AssetService.Contracts.TradingConditions;
using MarginTrading.Backend.Contracts;
using MarginTrading.TradingHistory.Client;
using Microsoft.Extensions.Internal;
using Microsoft.FeatureManagement;

namespace MarginTrading.AccountsManagement.Services.Implementation
{
    [UsedImplicitly]
    internal class AccountManagementService : IAccountManagementService
    {
        private readonly IAccountsRepository _accountsRepository;
        private readonly ITradingConditionsService _tradingConditionsService;
        private readonly ISendBalanceCommandsService _sendBalanceCommandsService;
        private readonly AccountManagementSettings _settings;
        private readonly IEventSender _eventSender;
        private readonly ILog _log;
        private readonly ISystemClock _systemClock;
        private readonly IAccountBalanceChangesRepository _accountBalanceChangesRepository;
        private readonly IDealsApi _dealsApi;
        private readonly IAccountsApi _accountsApi;
        private readonly IPositionsApi _positionsApi;
        private readonly ITradingInstrumentsApi _tradingInstrumentsApi;
        private readonly IEodTaxFileMissingRepository _taxFileMissingRepository;
        private readonly AccountsCache _cache;
        private readonly IFeatureManager _featureManager;
        private readonly IAuditService _auditService;

        public AccountManagementService(IAccountsRepository accountsRepository,
            ITradingConditionsService tradingConditionsService,
            ISendBalanceCommandsService sendBalanceCommandsService,
            AccountManagementSettings settings,
            IEventSender eventSender,
            ILog log,
            ISystemClock systemClock,
            AccountsCache cache, 
            IAccountBalanceChangesRepository accountBalanceChangesRepository, 
            IDealsApi dealsApi, 
            IEodTaxFileMissingRepository taxFileMissingRepository, 
            IAccountsApi accountsApi,
            IPositionsApi positionsApi, 
            ITradingInstrumentsApi tradingInstrumentsApi,
            IFeatureManager featureManager,
            IAuditService auditService)
        {
            _accountsRepository = accountsRepository;
            _tradingConditionsService = tradingConditionsService;
            _sendBalanceCommandsService = sendBalanceCommandsService;
            _settings = settings;
            _eventSender = eventSender;
            _log = log;
            _systemClock = systemClock;
            _cache = cache;
            _accountBalanceChangesRepository = accountBalanceChangesRepository;
            _dealsApi = dealsApi;
            _taxFileMissingRepository = taxFileMissingRepository;
            _accountsApi = accountsApi;
            _positionsApi = positionsApi;
            _tradingInstrumentsApi = tradingInstrumentsApi;
            _featureManager = featureManager;
            _auditService = auditService;
        }


        #region Create 

        public async Task<IAccount> CreateAsync(string clientId, 
            string accountId, 
            string tradingConditionId,
            string baseAssetId, 
            string accountName, 
            string userId)
        {
            #region Validations

            if (string.IsNullOrEmpty(tradingConditionId))
            {
                tradingConditionId = await _tradingConditionsService.GetDefaultTradingConditionIdAsync()
                    .RequiredNotNull("default trading condition");
            }

            var baseAssetExists = _tradingConditionsService.IsBaseAssetExistsAsync(tradingConditionId, baseAssetId);

            if (!await baseAssetExists)
            {
                throw new ArgumentOutOfRangeException(nameof(tradingConditionId),
                    $"Base asset [{baseAssetId}] is not configured for trading condition [{tradingConditionId}]");
            }

            var clientAccounts = await GetByClientAsync(clientId);

            if (!string.IsNullOrEmpty(accountId) && clientAccounts.Any(a => a.Id == accountId))
            {
                throw new NotSupportedException($"Client [{clientId}] already has account with ID [{accountId}]");
            }

            #endregion

            var legalEntity = await _tradingConditionsService.GetLegalEntityAsync(tradingConditionId);

            var account = await CreateAccount(clientId, baseAssetId, tradingConditionId, legalEntity, accountId, accountName, userId);

            _log.WriteInfo(nameof(AccountManagementService), nameof(CreateAsync),
                $"{baseAssetId} account {accountId} created for client {clientId} on trading condition {tradingConditionId}");

            return account;
        }

        public async Task<IReadOnlyList<IAccount>> CreateDefaultAccountsAsync(string clientId,
            string tradingConditionId)
        {
            var existingAccounts = (await _accountsRepository.GetAllAsync(clientId)).ToList();

            if (existingAccounts.Any())
            {
                return existingAccounts;
            }

            if (string.IsNullOrEmpty(tradingConditionId))
                throw new ArgumentNullException(nameof(tradingConditionId));

            var baseAssets = await _tradingConditionsService.GetBaseAccountAssetsAsync(tradingConditionId);
            var legalEntity = await _tradingConditionsService.GetLegalEntityAsync(tradingConditionId);

            var newAccounts = new List<IAccount>();

            foreach (var baseAsset in baseAssets)
            {
                try
                {
                    var account = await CreateAccount(clientId, baseAsset, tradingConditionId, legalEntity);
                    newAccounts.Add(account);
                }
                catch (Exception e)
                {
                    _log.WriteError(nameof(AccountManagementService),
                        $"Create default accounts: clientId={clientId}, tradingConditionId={tradingConditionId}", e);
                }
            }

            _log.WriteInfo(nameof(AccountManagementService), "CreateDefaultAccountsAsync",
                $"{string.Join(", ", newAccounts.Select(x => x.BaseAssetId))} accounts created for client {clientId}");

            return newAccounts;
        }

        public async Task<IReadOnlyList<IAccount>> CreateAccountsForNewBaseAssetAsync(string tradingConditionId,
            string baseAssetId)
        {
            var result = new List<IAccount>();

            var clientAccountGroups = (await _accountsRepository.GetAllAsync()).GroupBy(a => a.ClientId).Where(g =>
                g.Any(a => a.TradingConditionId == tradingConditionId) && g.All(a => a.BaseAssetId != baseAssetId));
            var legalEntity = await _tradingConditionsService.GetLegalEntityAsync(tradingConditionId);

            foreach (var group in clientAccountGroups)
            {
                try
                {
                    var account = await CreateAccount(group.Key, baseAssetId, tradingConditionId, legalEntity);
                    result.Add(account);
                }
                catch (Exception e)
                {
                    _log.WriteError(nameof(AccountManagementService),
                        $"Create accounts by account group : clientId={group.Key}, tradingConditionId={tradingConditionId}, baseAssetId={baseAssetId}",
                        e);
                }
            }

            _log.WriteInfo(nameof(AccountManagementService), nameof(CreateAccountsForNewBaseAssetAsync),
                $"{result.Count} accounts created for the new base asset {baseAssetId} in trading condition {tradingConditionId}");

            return result;
        }

        #endregion


        #region Get

        public Task<IReadOnlyList<IAccount>> ListAsync(string search, bool showDeleted = false)
        {
            return _accountsRepository.GetAllAsync(search: search, showDeleted: showDeleted);
        }

        public Task<PaginatedResponse<IAccount>> ListByPagesAsync(string search, bool showDeleted = false,
            int? skip = null, int? take = null, bool isAscendingOrder = true)
        {
            return _accountsRepository.GetByPagesAsync(search, showDeleted, skip, take, isAscendingOrder);
        }

        public Task<IReadOnlyList<IAccount>> GetByClientAsync(string clientId, bool showDeleted = false)
        {
            return _accountsRepository.GetAllAsync(clientId, showDeleted: showDeleted);
        }

        public Task<IAccount> GetByIdAsync(string accountId)
        {
            return _accountsRepository.GetAsync(accountId);
        }

        public async ValueTask<AccountStat> GetCachedAccountStatistics(string accountId)
        {
            if (string.IsNullOrEmpty(accountId))
                throw new ArgumentNullException(nameof(accountId));

            var onDate = _systemClock.UtcNow.UtcDateTime.Date; 

            var account = await _cache.Get(accountId, AccountsCache.Category.GetAccount, async() =>
            {
                var accfromDb = await _accountsRepository.GetAsync(accountId);

                return (value: accfromDb, shouldCache: accfromDb != null);
            });

            if (account == null)
            {
                return null;
            }

            var mtCoreAccountStats = await _accountsApi.GetAccountStats(accountId);

            var accountHistory = await _cache.Get(accountId, AccountsCache.Category.GetAccountBalanceChanges, () => _accountBalanceChangesRepository.GetAsync(accountId, onDate));

            var firstEvent = accountHistory.OrderByDescending(x => x.ChangeTimestamp).LastOrDefault();

            var accountCapital = await GetAccountCapitalAsync(account, mtCoreAccountStats, useCache: true); 
            
            var marginPercent = 0m;
            if (mtCoreAccountStats.TotalCapital != 0)
            {
                marginPercent = mtCoreAccountStats.UsedMargin / mtCoreAccountStats.TotalCapital * 100;
            }

            var result = new AccountStat(
                accountId,
                _systemClock.UtcNow.UtcDateTime,
                accountHistory.GetTotalByType(AccountBalanceChangeReasonType.RealizedPnL),
                unRealisedPnl: accountHistory.GetTotalByType(AccountBalanceChangeReasonType.UnrealizedDailyPnL),
                depositAmount: accountHistory.GetTotalByType(AccountBalanceChangeReasonType.Deposit),
                withdrawalAmount: accountHistory.GetTotalByType(AccountBalanceChangeReasonType.Withdraw),
                commissionAmount: accountHistory.GetTotalByType(AccountBalanceChangeReasonType.Commission),
                otherAmount: accountHistory.Where(x => !new[]
                {
                    AccountBalanceChangeReasonType.RealizedPnL,
                    AccountBalanceChangeReasonType.Deposit,
                    AccountBalanceChangeReasonType.Withdraw,
                    AccountBalanceChangeReasonType.Commission,
                }.Contains(x.ReasonType)).Sum(x => x.ChangeAmount),
                accountBalance: account.Balance,
                prevEodAccountBalance: (firstEvent?.Balance - firstEvent?.ChangeAmount) ?? account.Balance,
                disposableCapital: accountCapital.Disposable,
                accountName: account.AccountName,
                accountCapitalDetails: accountCapital,
                totalCapital: mtCoreAccountStats.TotalCapital,
                usedMargin: mtCoreAccountStats.UsedMargin,
                usedMarginPercent: marginPercent,
                freeMargin: mtCoreAccountStats.FreeMargin,
                pnl: mtCoreAccountStats.PnL,
                balance: mtCoreAccountStats.Balance,
                unrealizedPnlDaily: mtCoreAccountStats.UnrealizedDailyPnl,
                currentlyUsedMargin: mtCoreAccountStats.CurrentlyUsedMargin,
                initiallyUsedMargin: mtCoreAccountStats.InitiallyUsedMargin,
                openPositionsCount: mtCoreAccountStats.OpenPositionsCount,
                lastBalanceChangeTime: mtCoreAccountStats.LastBalanceChangeTime,
                additionalInfo: account.AdditionalInfo.Serialize()
            );

            return result;
        }

        /// <summary>
        /// By valid it means account exists and not deleted.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="skipDeleteValidation"></param>
        /// <returns>Account</returns>
        public async Task<IAccount> EnsureAccountValidAsync(string accountId, bool skipDeleteValidation = false)
        {
            var account = await GetByIdAsync(accountId);

            account.RequiredNotNull(nameof(account), $"Account [{accountId}] does not exist");

            if (!skipDeleteValidation && account.IsDeleted)
            {
                throw new ValidationException(
                    $"Account [{account.Id}] is deleted. No operations are permitted.");
            }

            return account;
        }

        public async Task<AccountCapital> GetAccountCapitalAsync(string accountId, bool useCache)
        {
            var account = await _accountsRepository.GetAsync(accountId);
            var mtCoreAccountStats = await _accountsApi.GetAccountStats(accountId);

            return await GetAccountCapitalAsync(account, mtCoreAccountStats, useCache);
        }

        public Task<PaginatedResponse<IClient>> ListClientsByPagesAsync(string tradingConditionId, int skip, int take)
        {
            return _accountsRepository.GetClientsByPagesAsync(tradingConditionId, skip, take);
        }

        public Task<PaginatedResponse<IClientSearchResult>> SearchByClientAsync(string query, int skip, int take) =>
            _accountsRepository.SearchByClientAsync(query, skip, take);

        public Task<IEnumerable<IClient>> GetAllClients()
        {
            return _accountsRepository.GetAllClients();
        }

        public Task<IClient> GetClient(string clientId)
        {
            return _accountsRepository.GetClient(clientId);
        }

        private async Task<AccountCapital> GetAccountCapitalAsync(IAccount account, Backend.Contracts.Account.AccountStatContract mtCoreAccountStat, bool useCache)
        {
            if (account == null)
                throw new ArgumentNullException(nameof(account));

            var temporaryCapital = account.GetTemporaryCapital();
            
            var realizedProfit = await GetRealizedProfit(account.Id, useCache);
            var unRealizedProfit = await GetUnrealizedProfit(account.Id);
            
            return new AccountCapital(
                account.Balance, 
                mtCoreAccountStat.TotalCapital,
                totalRealisedPnl: realizedProfit.total,
                unRealizedProfit,
                temporaryCapital, 
                compensations: realizedProfit.compensations,
                account.BaseAssetId,
                usedMargin: mtCoreAccountStat.UsedMargin);
        }

        #endregion

        #region Modify

        public async Task<IAccount> UpdateAccountAsync(string accountId, bool? isDisabled, bool? isWithdrawalDisabled)
        {
            if (isDisabled ?? false)
            {
                await ValidateStatsBeforeDisableAsync(accountId);
            }

            var account = await EnsureAccountValidAsync(accountId, true);

            var result =
                await _accountsRepository.UpdateAccountAsync(
                    accountId,
                    isDisabled,
                    isWithdrawalDisabled);

            _eventSender.SendAccountChangedEvent(
                nameof(UpdateAccountAsync),
                result,
                AccountChangedEventTypeContract.Updated,
                Guid.NewGuid().ToString("N"),
                previousSnapshot: account);

            return result;
        }

        public async Task ResetAccountAsync(string accountId)
        {
            if (_settings.Behavior?.BalanceResetIsEnabled != true)
            {
                throw new NotSupportedException("Account reset is not supported");
            }

            var account = await EnsureAccountValidAsync(accountId, true);

            await UpdateBalanceAsync(Guid.NewGuid().ToString(), accountId,
                _settings.Behavior.DefaultBalance - account.Balance, AccountBalanceChangeReasonType.Reset,
                "Reset account Api");

            await _accountsRepository.EraseAsync(accountId);
        }

        public async Task<string> StartGiveTemporaryCapital(string eventSourceId, string accountId, decimal amount,
            string reason, string comment, string additionalInfo)
        {
            return await _sendBalanceCommandsService.GiveTemporaryCapital(
                eventSourceId,
                accountId,
                amount,
                reason,
                comment,
                additionalInfo);
        }

        public async Task<string> StartRevokeTemporaryCapital(string eventSourceId, string accountId,
            string revokeEventSourceId, string comment, string additionalInfo)
        {
            return await _sendBalanceCommandsService.RevokeTemporaryCapital(
                eventSourceId,
                accountId,
                revokeEventSourceId,
                comment,
                additionalInfo);
        }

        public Task ClearStatsCache(string accountId)
        {
            if (string.IsNullOrEmpty(accountId))
                throw new ArgumentNullException(nameof(accountId));

            return _cache.Invalidate(accountId);
        }

        public async Task<Result<TradingConditionErrorCodes>> UpdateClientTradingCondition(string clientId, string tradingConditionId, string username, string correlationId)
        {
            if (!await _tradingConditionsService.IsTradingConditionExistsAsync(tradingConditionId))
            {
                _log.WriteWarning(nameof(AccountManagementService), nameof(tradingConditionId), $"{tradingConditionId} does not exist");

                return new Result<TradingConditionErrorCodes>(TradingConditionErrorCodes.TradingConditionNotFound);
            }

            var beforeUpdate = (await _accountsRepository.GetAllAsync(clientId))
                .ToDictionary(p=>p.Id);

            foreach (var accountId in beforeUpdate.Keys)
            {
                var positions = await _positionsApi.ListAsyncByPages(accountId);
                var productIds = positions.Contents.Select(x => x.AssetPairId).Distinct().ToList();
                var getUnavailableProductsResponse = await _tradingInstrumentsApi.CheckProductsUnavailableForTradingCondition(
                    new CheckProductsUnavailableForTradingConditionRequest()
                    {
                        ProductIds = productIds,
                        TradingConditionId = tradingConditionId,
                    });

                var unavailableProducts = getUnavailableProductsResponse.UnavailableProductIds;
                if (unavailableProducts.Count > 0)
                {
                    _log.WriteWarning(nameof(AccountManagementService), nameof(accountId), $"Client {clientId} has open positions for account {accountId}. List of unavailable products: {string.Join(", ", unavailableProducts)}");
                    return new Result<TradingConditionErrorCodes>(TradingConditionErrorCodes.ClientHasOpenPositions);
                }
            }

            var clientToAudit = await _accountsRepository.GetClient(clientId, true);
            await _accountsRepository.UpdateClientTradingCondition(clientId, tradingConditionId);

            var afterUpdate = await _accountsRepository.GetAllAsync(clientId);

            foreach (var account in afterUpdate)
            {
                _eventSender.SendAccountChangedEvent(
                    nameof(UpdateClientTradingCondition),
                    account,
                    AccountChangedEventTypeContract.Updated,
                    Guid.NewGuid().ToString("N"),
                    previousSnapshot: beforeUpdate[account.Id]);
            }
            
            await _auditService.TryAuditTradingConditionUpdate(correlationId,
                username,
                clientId,
                tradingConditionId,
                clientToAudit.TradingConditionId);
            
            return new Result<TradingConditionErrorCodes>();
        }

        public async Task<Result<TradingConditionErrorCodes>> UpdateClientTradingConditions(IReadOnlyList<(string clientId, string tradingConditionId)> updates, string username, string correlationId)
        {
            var clientsInDb =  (await _accountsRepository.GetClients(updates.Select(p => p.clientId)))
                .ToDictionary(p => p.Id);
            
            foreach (var (clientId, tradingConditionId) in updates)
            {
                if (!clientsInDb.TryGetValue(clientId, out var existedClient))
                {
                    _log.WriteWarning(nameof(AccountManagementService),
                        new {clientId}.ToJson(), 
                        $"Client {clientId} not exist");
                    return new Result<TradingConditionErrorCodes>(TradingConditionErrorCodes.ClientNotFound);
                }

                if (existedClient.TradingConditionId == tradingConditionId)
                {
                    continue;
                }

                var tradingConditionUpdateResult = await UpdateClientTradingCondition(clientId, tradingConditionId, username, correlationId);
                if (tradingConditionUpdateResult.IsFailed)
                    return tradingConditionUpdateResult;
            }

            return new Result<TradingConditionErrorCodes>();
        }

        #region ComplexityWarning


        public async Task UpdateComplexityWarningFlag(string accountId, bool shouldShowProductComplexityWarning,
            string orderId = null)
        {
            var previousSnapshot = await EnsureAccountValidAsync(accountId, skipDeleteValidation: true);
            if (previousSnapshot.IsDeleted) return;

            var updated= await _accountsRepository.UpdateAdditionalInfo(previousSnapshot.Id, s =>
                {
                    s.ShouldShowProductComplexityWarning = shouldShowProductComplexityWarning;
                });

            _eventSender.SendAccountChangedEvent(
                nameof(UpdateComplexityWarningFlag),
                updated,
                AccountChangedEventTypeContract.Updated,
                Guid.NewGuid().ToString("N"),
                previousSnapshot: previousSnapshot,
                orderId: orderId);
        }

        #endregion

        #endregion

        #region Helpers

        private async Task<IAccount> CreateAccount(string clientId, 
            string baseAssetId, 
            string tradingConditionId,
            string legalEntityId, 
            string accountId = null, 
            string accountName = null, 
            string userId = null)
        {
            var id = string.IsNullOrEmpty(accountId)
                ? $"{_settings.Behavior?.AccountIdPrefix}{Guid.NewGuid():N}"
                : accountId;

            var shouldShowProductComplexityWarning =  await _featureManager.IsEnabledAsync(BrokerFeature.ProductComplexityWarning) ? (bool?) true : null;
            
            IAccount account = new Account(
                id,
                clientId,
                tradingConditionId,
                baseAssetId,
                0,
                0,
                legalEntityId,
                false,
                !(_settings.Behavior?.DefaultWithdrawalIsEnabled ?? true),
                false,
                DateTime.UtcNow,
                accountName,
                userId,
                new AccountAdditionalInfo
                {
                    ShouldShowProductComplexityWarning = shouldShowProductComplexityWarning
                });

            await _accountsRepository.AddAsync(account);
            account = await _accountsRepository.GetAsync(accountId);

            _eventSender.SendAccountChangedEvent(nameof(CreateAccount), account,
                AccountChangedEventTypeContract.Created, id);

            //todo consider moving to CQRS projection
            if (_settings.Behavior?.DefaultBalance != null && _settings.Behavior.DefaultBalance != default)
            {
                await UpdateBalanceAsync(Guid.NewGuid().ToString(), account.Id, _settings.Behavior.DefaultBalance,
                    AccountBalanceChangeReasonType.Create, "Create account Api");
            }

            return account;
        }

        private async Task UpdateBalanceAsync(string operationId, string accountId,
            decimal amountDelta, AccountBalanceChangeReasonType changeReasonType, string source, bool changeTransferLimit = false)
        {
            await _sendBalanceCommandsService.ChargeManuallyAsync(
                accountId,
                amountDelta,
                operationId,
                changeReasonType.ToString(),
                source,
                null,
                changeReasonType,
                operationId,
                null,
                _systemClock.UtcNow.UtcDateTime);
        }
        
        private async Task ValidateStatsBeforeDisableAsync(string accountId)
        {
            var stats = await _accountsApi.GetAccountStats(accountId);
            if (stats.ActiveOrdersCount > 0 || stats.OpenPositionsCount > 0)
            {
                throw new DisableAccountWithPositionsOrOrdersException();
            }
        }

        public static List<TemporaryCapital> UpdateTemporaryCapital(string accountId, List<TemporaryCapital> current,
            TemporaryCapital temporaryCapital, bool isAdd)
        {
            var result = current.ToList();

            if (isAdd)
            {
                if (result.Any(x => x.Id == temporaryCapital.Id))
                {
                    throw new ArgumentException(
                        $"Temporary capital record with id {temporaryCapital.Id} is already set on account {accountId}",
                        nameof(temporaryCapital.Id));
                }

                if (temporaryCapital != null)
                {
                    result.Add(temporaryCapital);
                }
            }
            else
            {
                if (temporaryCapital != null)
                {
                    result.RemoveAll(x => x.Id == temporaryCapital.Id);
                }
                else
                {
                    result.Clear();
                }
            }

            return result;
        }

        private async Task<(decimal deals, decimal compensations, decimal dividends, decimal total)> GetRealizedProfit(string accountId, bool useCache)
        {
            //@avolkov for some use cases (in message handlers) we should not use cache 
            //to not duplicate logic added this ugly hack that will read from db in message handlers and will use cache in http calls
            Task<T> GetCachedIfAllowed<T>(AccountsCache.Category cat, Func<Task<T>> getValue)
            {
                return useCache ? _cache.Get(accountId, cat, getValue) : getValue();
            }

            var taxFileMissingDays = await GetCachedIfAllowed(AccountsCache.Category.GetTaxFileMissingDays, () => _taxFileMissingRepository.GetAllDaysAsync());

            var missingDaysArray = taxFileMissingDays as DateTime[] ?? taxFileMissingDays.ToArray();
            
            LogWarningForTaxFileMissingDaysIfRequired(accountId, missingDaysArray);
            
            var getDeals = await GetCachedIfAllowed(AccountsCache.Category.GetDeals, () => _dealsApi.GetTotalProfit(accountId, missingDaysArray));
            var deals = getDeals?.Value ?? 0;
            var compensations =  await GetCachedIfAllowed(AccountsCache.Category.GetCompensations, () => _accountBalanceChangesRepository.GetCompensationsProfit(accountId, missingDaysArray));
            var dividends =  await GetCachedIfAllowed(AccountsCache.Category.GetDividends, () => _accountBalanceChangesRepository.GetDividendsProfit(accountId, missingDaysArray));

            var total = deals + compensations + dividends;

            return (deals, compensations, dividends, total);
        }

        private async Task<decimal> GetUnrealizedProfit(string accountId)
        {
            var openPositions = await _positionsApi.ListAsync(accountId);

            return openPositions.Where(p => p.PnL > 0).Sum(p => p.PnL);
        }

        private void LogWarningForTaxFileMissingDaysIfRequired(string accountId, DateTime[] missingDays)
        {
            if (missingDays.Length > 1)
            {
                _log.WriteWarning(nameof(AccountManagementService),
                    new {accountId, missingDays}.ToJson(),
                    "There are days which we don't have tax file for. Therefore these days PnL will be excluded from total PnL for the account.");
            }
        }

        #endregion
    }
}