// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Logging;
using JetBrains.Annotations;
using Lykke.Snow.Common.Correlation;
using Lykke.Snow.Common.Model;
using Lykke.Snow.Common.Percents;
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
        private readonly ILogger _logger;
        private readonly ISystemClock _systemClock;
        private readonly IAccountBalanceChangesRepository _accountBalanceChangesRepository;
        private readonly IDealsApi _dealsApi;
        private readonly IAccountsApi _accountsApi;
        private readonly IPositionsApi _positionsApi;
        private readonly ITradingInstrumentsApi _tradingInstrumentsApi;
        private readonly IEodTaxFileMissingRepository _taxFileMissingRepository;
        private readonly IAccountsCache _cache;
        private readonly IFeatureManager _featureManager;
        private readonly IAuditService _auditService;
        private readonly CorrelationContextAccessor _correlationContextAccessor;
        private readonly IBrokerSettingsCache _brokerSettingsCache;

        public AccountManagementService(IAccountsRepository accountsRepository,
            ITradingConditionsService tradingConditionsService,
            ISendBalanceCommandsService sendBalanceCommandsService,
            AccountManagementSettings settings,
            IEventSender eventSender,
            ILogger<AccountManagementService> logger,
            ISystemClock systemClock,
            IAccountsCache cache, 
            IAccountBalanceChangesRepository accountBalanceChangesRepository, 
            IDealsApi dealsApi, 
            IEodTaxFileMissingRepository taxFileMissingRepository, 
            IAccountsApi accountsApi,
            IPositionsApi positionsApi, 
            ITradingInstrumentsApi tradingInstrumentsApi,
            IFeatureManager featureManager,
            IAuditService auditService,
            CorrelationContextAccessor correlationContextAccessor,
            IBrokerSettingsCache brokerSettingsCache)
        {
            _accountsRepository = accountsRepository;
            _tradingConditionsService = tradingConditionsService;
            _sendBalanceCommandsService = sendBalanceCommandsService;
            _settings = settings;
            _eventSender = eventSender;
            _logger = logger;
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
            _correlationContextAccessor = correlationContextAccessor;
            _brokerSettingsCache = brokerSettingsCache;
        }


        #region Create 

        public async Task<IAccount> CreateAsync(string clientId, 
            string accountId, 
            string tradingConditionId,
            string baseAssetId, 
            string accountName, 
            string userId,
            string referenceAccount)
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

            var account = await CreateAccount(clientId,
                baseAssetId,
                tradingConditionId,
                legalEntity,
                referenceAccount,
                accountId,
                accountName,
                userId);

            _logger.LogInformation(
                "{BaseAssetId} account {AccountId} created for client {ClientId} on trading condition {TradingConditionId}",
                baseAssetId, accountId, clientId, tradingConditionId);

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
                    var account = await CreateAccount(clientId, baseAsset, tradingConditionId, legalEntity, string.Empty);
                    newAccounts.Add(account);
                }
                catch (Exception e)
                {
                    _logger.LogError(e,
                        "Create default accounts: clientId={ClientId}, tradingConditionId={TradingConditionId}",
                        clientId, tradingConditionId);
                }
            }

            _logger.LogInformation("{AccountIds} accounts created for client {ClientId}",
                string.Join(", ", newAccounts.Select(x => x.Id)), clientId);

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
                    var account = await CreateAccount(group.Key, baseAssetId, tradingConditionId, legalEntity, string.Empty);
                    result.Add(account);
                }
                catch (Exception e)
                {
                    _logger.LogError(e,
                        "Create accounts by account group: clientId={ClientId}, tradingConditionId={TradingConditionId}, baseAssetId={BaseAssetId}",
                        group.Key, tradingConditionId, baseAssetId);
                }
            }

            _logger.LogInformation(
                "{AccountsCount} accounts created for the new base asset {BaseAssetId} in trading condition {TradingConditionId}",
                result.Count, baseAssetId, tradingConditionId);

            return result;
        }

        #endregion


        #region Get

        public Task<IReadOnlyList<IAccountSuggested>> SuggestedListAsync(string query, int limit)
        {
            return _accountsRepository.GetSuggestedListAsync(query, limit);
        }

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

        /// <summary>
        /// Gets account statistics from cache
        /// </summary>
        /// <param name="accountId">The account id</param>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException">When account id is empty or
        /// <see cref="AccountCapital"/> can't be instantiated because of
        /// some parameters are empty.</exception>
        public async ValueTask<AccountStat> GetCachedAccountStatistics(string accountId)
        {
            if (string.IsNullOrEmpty(accountId))
                throw new ArgumentNullException(nameof(accountId));

            var mtCoreAccountStats = await _accountsApi.GetCapitalFigures(accountId);

            var baseAssetIdAndTemporaryCapital = await _cache.Get(accountId, AccountsCacheCategory.GetBaseAssetIdAndTemporaryCapital, async() =>
            {
                var baseAssetIdAndTemporaryCapitalFromDb = await _accountsRepository.GetBaseAssetIdAndTemporaryCapitalAsync(accountId);

                return (value: baseAssetIdAndTemporaryCapitalFromDb, shouldCache: !string.IsNullOrWhiteSpace(baseAssetIdAndTemporaryCapitalFromDb.baseAssetId));
            });

            var accountCapital = await GetAccountCapitalAsync(accountId,
                baseAssetIdAndTemporaryCapital.baseAssetId,
                baseAssetIdAndTemporaryCapital.temporaryCapital,
                mtCoreAccountStats.Balance,
                mtCoreAccountStats.TotalCapital,
                mtCoreAccountStats.UsedMargin,
                useCache: true);

            var result = new AccountStat(
                mtCoreAccountStats.TodayRealizedPnL,
                unRealisedPnl: mtCoreAccountStats.TodayUnrealizedPnL,
                depositAmount: mtCoreAccountStats.TodayDepositAmount,
                withdrawalAmount: mtCoreAccountStats.TodayWithdrawAmount,
                commissionAmount: mtCoreAccountStats.TodayCommissionAmount,
                otherAmount: mtCoreAccountStats.TodayOtherAmount,
                prevEodAccountBalance: mtCoreAccountStats.TodayStartBalance,
                disposableCapital: accountCapital.Disposable,
                totalCapital: mtCoreAccountStats.TotalCapital,
                usedMargin: mtCoreAccountStats.UsedMargin,
                freeMargin: mtCoreAccountStats.FreeMargin,
                pnl: mtCoreAccountStats.PnL,
                balance: mtCoreAccountStats.Balance,
                unrealizedPnlDaily: mtCoreAccountStats.UnrealizedDailyPnl,
                currentlyUsedMargin: mtCoreAccountStats.CurrentlyUsedMargin,
                initiallyUsedMargin: mtCoreAccountStats.InitiallyUsedMargin,
                openPositionsCount: mtCoreAccountStats.OpenPositionsCount,
                lastBalanceChangeTime: mtCoreAccountStats.LastBalanceChangeTime,
                additionalInfo: mtCoreAccountStats.AdditionalInfo,
                temporaryCapital: accountCapital.Temporary
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
            var baseAssetIdAndTemporaryCapital =
                await _accountsRepository.GetBaseAssetIdAndTemporaryCapitalAsync(accountId);

            var mtCoreAccountStats = await _accountsApi.GetAccountStats(accountId);

            // todo: implement as decorator
            _logger.LogInformation("MT Core account stats for account {AccountId}: {AccountStatsJson}", 
                accountId,
                mtCoreAccountStats.ToJson());

            return await GetAccountCapitalAsync(accountId,
                baseAssetIdAndTemporaryCapital.baseAssetId,
                baseAssetIdAndTemporaryCapital.temporaryCapital,
                mtCoreAccountStats.Balance,
                mtCoreAccountStats.TotalCapital,
                mtCoreAccountStats.UsedMargin,
                useCache);
        }

        public Task<PaginatedResponse<IClient>> ListClientsByPagesAsync(string tradingConditionId, int skip, int take)
        {
            return _accountsRepository.GetClientsByPagesAsync(tradingConditionId, skip, take);
        }

        public Task<PaginatedResponse<IClientWithAccounts>> SearchByClientAsync(string query, int skip, int take) =>
            _accountsRepository.SearchByClientAsync(query, skip, take);

        public Task<IEnumerable<IClient>> GetAllClients()
        {
            return _accountsRepository.GetAllClients();
        }

        public Task<IEnumerable<IClientWithAccounts>> GetAllClientsWithAccounts()
        {
            return _accountsRepository.GetAllClientsWithAccounts();
        }

        public Task<IClient> GetClient(string clientId)
        {
            return _accountsRepository.GetClient(clientId);
        }

        private async Task<AccountCapital> GetAccountCapitalAsync(string accountId,
            string baseAssetId, decimal temporaryCapital, decimal balance, decimal totalCapital, decimal usedMargin, bool useCache)
        {
            if (string.IsNullOrWhiteSpace((accountId)))
                throw new ArgumentNullException(nameof(accountId));
                
            var realizedProfitTask = GetRealizedProfit(accountId, useCache);
            var unRealizedProfitTask = GetUnrealizedProfit(accountId);

            await Task.WhenAll(realizedProfitTask, unRealizedProfitTask);

            var realizedProfit = await realizedProfitTask;
            _logger.LogInformation("Realized profit for account {AccountId} is {RealizedProfitJson}", 
                accountId, 
                realizedProfit.ToJson());
            
            var unRealizedProfit = await unRealizedProfitTask;
            _logger.LogInformation("Unrealized profit for account {AccountId} is {UnrealizedProfit}", 
                accountId, 
                unRealizedProfit);

            var disposableCapitalWithholdPercent = new Percent(_brokerSettingsCache.Get().DisposableCapitalWithholdPercent);
            _logger.LogInformation("Disposable capital withhold percent for account {AccountId} is {DisposableCapitalWithholdPercent}", 
                accountId, 
                disposableCapitalWithholdPercent.ToString());
            
            var result = new AccountCapital(
                balance, 
                totalCapital,
                unRealizedProfit,
                temporaryCapital,
                realizedProfit.deals,
                realizedProfit.compensations,
                realizedProfit.dividends,
                baseAssetId,
                usedMargin,
                disposableCapitalWithholdPercent);

            return result;
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

        public async Task<Result<TradingConditionErrorCodes>> UpdateClientTradingCondition(string clientId, string tradingConditionId, string username)
        {
            if (!await _tradingConditionsService.IsTradingConditionExistsAsync(tradingConditionId))
            {
                _logger.LogWarning("{TradingConditionId} does not exist", tradingConditionId);
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
                    _logger.LogWarning(
                        "Client {ClientId} has open positions for account {AccountId}. List of unavailable products: {UnavailableProducts}",
                        clientId, accountId, string.Join(", ", unavailableProducts));
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

            var correlationId = _correlationContextAccessor.CorrelationContext?.CorrelationId ??
                                Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
            await _auditService.TryAuditTradingConditionUpdate(correlationId,
                username,
                clientId,
                tradingConditionId,
                clientToAudit.TradingConditionId);
            
            return new Result<TradingConditionErrorCodes>();
        }

        public async Task<Result<TradingConditionErrorCodes>> UpdateClientTradingConditions(IReadOnlyList<(string clientId, string tradingConditionId)> updates, string username)
        {
            var clientsInDb =  (await _accountsRepository.GetClients(updates.Select(p => p.clientId)))
                .ToDictionary(p => p.Id);
            
            foreach (var (clientId, tradingConditionId) in updates)
            {
                if (!clientsInDb.TryGetValue(clientId, out var existedClient))
                {
                    _logger.LogWarning("Client {ClientId} not exist", clientId);
                    return new Result<TradingConditionErrorCodes>(TradingConditionErrorCodes.ClientNotFound);
                }

                if (existedClient.TradingConditionId == tradingConditionId)
                {
                    continue;
                }

                var tradingConditionUpdateResult = await UpdateClientTradingCondition(clientId, tradingConditionId, username);
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


        #region 871mWarning

        public async Task Update871mWarningFlag(string accountId, bool shouldShow871mWarning,
            string orderId = null)
        {
            var previousSnapshot = await EnsureAccountValidAsync(accountId, skipDeleteValidation: true);
            if (previousSnapshot.IsDeleted) return;

            var updated= await _accountsRepository.UpdateAdditionalInfo(previousSnapshot.Id, s =>
            {
                s.ShouldShow871mWarning = shouldShow871mWarning;
            });

            _eventSender.SendAccountChangedEvent(
                nameof(Update871mWarningFlag),
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
            string referenceAccount,
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
                },
                referenceAccount);

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
            Task<T> GetCachedIfAllowed<T>(AccountsCacheCategory cat, Func<Task<T>> getValue)
            {
                return useCache ? _cache.Get(accountId, cat, getValue) : getValue();
            }

            var taxFileMissingDays = await GetCachedIfAllowed(AccountsCacheCategory.GetTaxFileMissingDays, () => _taxFileMissingRepository.GetAllDaysAsync());

            var missingDaysArray = taxFileMissingDays == null
                ? Array.Empty<DateTime>()
                : taxFileMissingDays.ToArray();

            LogWarningForTaxFileMissingDaysIfRequired(accountId, missingDaysArray);
            
            var getDealsTask = GetCachedIfAllowed(AccountsCacheCategory.GetDeals, () => _dealsApi.GetTotalProfit(accountId, missingDaysArray));
            var compensationsTask = GetCachedIfAllowed(AccountsCacheCategory.GetCompensations, () => _accountBalanceChangesRepository.GetCompensationsProfit(accountId, missingDaysArray));
            var dividendsTask = GetCachedIfAllowed(AccountsCacheCategory.GetDividends, () => _accountBalanceChangesRepository.GetDividendsProfit(accountId, missingDaysArray));

            await Task.WhenAll(getDealsTask, compensationsTask, dividendsTask);

            var getDeals = await getDealsTask;
            var compensations = await compensationsTask;
            var dividends = await dividendsTask;
            
            var deals = getDeals?.Value ?? 0;

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
                _logger.LogWarning(
                    "There are days which we don't have tax file for. Therefore these days PnL will be excluded from total PnL for the account: [{Days}]",
                    new { accountId, missingDays }.ToJson());
            }
        }

        #endregion
    }
}