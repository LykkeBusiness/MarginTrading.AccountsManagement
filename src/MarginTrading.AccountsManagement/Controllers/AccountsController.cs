// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Common;
using Common.Log;
using JetBrains.Annotations;
using Lykke.Snow.Common.Startup;
using Lykke.Snow.Mdm.Contracts.Api;
using Lykke.Snow.Mdm.Contracts.Models.Contracts;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.AccountsManagement.Extensions;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AssetService.Contracts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Internal;
using Refit;
using MarginTrading.AccountsManagement.Exceptions;
using MarginTrading.AccountsManagement.Contracts.ErrorCodes;
using MarginTrading.AccountsManagement.Infrastructure.Implementation;
using MarginTrading.AccountsManagement.InternalModels.ErrorCodes;

namespace MarginTrading.AccountsManagement.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/accounts")]
    public class AccountsController : Controller
    {
        private readonly IAccountManagementService _accountManagementService;
        private readonly IAccuracyService _accuracyService;
        private readonly IConvertService _convertService;
        private readonly ISystemClock _systemClock;
        private readonly ISendBalanceCommandsService _sendBalanceCommandsService;
        private readonly ICqrsSender _cqrsSender;
        private readonly IScheduleSettingsApi _scheduleSettingsApi;
        private readonly IBrokerSettingsApi _brokerSettingsApi;
        private readonly BrokerConfigurationAccessor _brokerConfigurationAccessor;
        private readonly ILog _logger;

        public AccountsController(
            IAccountManagementService accountManagementService,
            IAccuracyService accuracyService,
            IConvertService convertService,
            ISystemClock systemClock,
            ISendBalanceCommandsService sendBalanceCommandsService,
            ICqrsSender cqrsSender,
            IScheduleSettingsApi scheduleSettingsApi, 
            IBrokerSettingsApi brokerSettingsApi, 
            BrokerConfigurationAccessor brokerConfigurationAccessor,
            ILog logger)
        {
            _accountManagementService = accountManagementService;
            _accuracyService = accuracyService;
            _convertService = convertService;
            _systemClock = systemClock;
            _sendBalanceCommandsService = sendBalanceCommandsService;
            _cqrsSender = cqrsSender;
            _scheduleSettingsApi = scheduleSettingsApi;
            _brokerSettingsApi = brokerSettingsApi;
            _brokerConfigurationAccessor = brokerConfigurationAccessor;
            _logger = logger;
        }

        #region CRUD
        
        [HttpGet]
        [Route("suggested")]
        public async Task<List<AccountSuggestedContract>> SuggestedList([FromQuery, Required, MinLength(2)] string query, [FromQuery] int limit)
        {
            var items = await _accountManagementService.SuggestedListAsync(query, limit);
            return items.Select(Convert).ToList();
        }

        /// <summary>
        /// Gets all accounts
        /// </summary>
        [HttpGet]
        [Route("")]
        public Task<List<AccountContract>> List([FromQuery] string search = null, bool showDeleted = false)
        {
            return Convert(_accountManagementService.ListAsync(search, showDeleted));
        }

        /// <summary>
        /// Gets all accounts, optionally paginated. Both skip and take must be set or unset.
        /// </summary>ListByPages
        [HttpGet]
        [Route("by-pages")]
        [Obsolete("Consider using search API")]
        public Task<Contracts.PaginatedResponseContract<AccountContract>> ListByPages([FromQuery] string search = null,
            [FromQuery] int? skip = null, [FromQuery] int? take = null, bool showDeleted = false, bool isAscendingOrder = true)
        {
            if ((skip.HasValue && !take.HasValue) || (!skip.HasValue && take.HasValue))
            {
                throw new ArgumentOutOfRangeException(nameof(skip), "Both skip and take must be set or unset");
            }

            if (take.HasValue && (take <= 0 || skip < 0))
            {
                throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be >= 0, take must be > 0");
            }

            return Convert(_accountManagementService.ListByPagesAsync(search, showDeleted, skip, take, isAscendingOrder));
        }

        /// <summary>
        /// Gets all accounts by <paramref name="clientId"/>
        /// </summary>
        [HttpGet]
        [Route("{clientId}")]
        public Task<List<AccountContract>> GetByClient([NotNull] string clientId, bool showDeleted = false)
        {
            return Convert(_accountManagementService.GetByClientAsync(
                clientId.RequiredNotNullOrWhiteSpace(nameof(clientId)), showDeleted));
        }

        /// <summary>
        /// Gets account by clientId and accountId
        /// </summary>
        [HttpGet]
        [Obsolete("Use GetById.")]
        [Route("{clientId}/{accountId}")]
        public Task<AccountContract> GetByClientAndId([NotNull] string clientId, [NotNull] string accountId)
        {
            return GetById(accountId);
        }

        /// <summary>
        /// Gets account by accountId
        /// </summary>
        [HttpGet]
        [Route("by-id/{accountId}")]
        public Task<AccountContract> GetById([NotNull] string accountId)
        {
            return Convert(_accountManagementService.GetByIdAsync(accountId.RequiredNotNullOrWhiteSpace(nameof(accountId))));
        }

        /// <summary>
        /// Search clients by clientId, account name (first) or account (second) id on partial matching
        /// </summary>
        /// <param name="query">The search string</param>
        /// <param name="skip">How many items to skip</param>
        /// <param name="take">How many items to take</param>
        /// <returns>The list of clients with trading condition and account names matching the search criteria</returns>
        [HttpGet]
        [Route("client-trading-conditions/search/by-client")]
        public async Task<Contracts.PaginatedResponseContract<ClientTradingConditionsSearchResultContract>>
            SearchByClient([FromQuery] string query, [FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            if (take <= 0 || skip < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be >= 0, take must be > 0");
            }
            
            var result = await _accountManagementService.SearchByClientAsync(query, skip, take);

            return new Contracts.PaginatedResponseContract<ClientTradingConditionsSearchResultContract>(
                result.Contents.Select(x => _convertService.Convert<IClientSearchResult, ClientTradingConditionsSearchResultContract>(x)).ToList(),
                result.Start,
                result.Size,
                result.TotalSize
            );
        }

        /// <summary>
        /// Gets client trading conditions
        /// </summary>
        [HttpGet]
        [Route("client-trading-conditions/{clientId}")]
        public async Task<ClientTradingConditionsContract> GetClientTradingConditions([NotNull] string clientId)
        {
            var result = await _accountManagementService.GetClient(clientId.RequiredNotNullOrWhiteSpace(nameof(clientId)));

            return _convertService.Convert<IClient, ClientTradingConditionsContract>(result);
        }

        /// <summary>
        /// Gets paginated client trading conditions optionally filtered by trading condition. Both skip and take must be set or unset.
        /// </summary>
        [HttpGet]
        [Route("client-trading-conditions")]
        public async Task<Contracts.PaginatedResponseContract<ClientTradingConditionsContract>> ListClientsTradingConditions([FromQuery] string tradingConditionId, [FromQuery] int skip = 0, [FromQuery] int take = 20)
        {
            if (take <= 0 || skip < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(skip), "Skip must be >= 0, take must be > 0");
            }

            var result = await _accountManagementService.ListClientsByPagesAsync(tradingConditionId, skip, take);
            return new Contracts.PaginatedResponseContract<ClientTradingConditionsContract>(
                result.Contents.Select(x=> _convertService.Convert<IClient,ClientTradingConditionsContract>(x)).ToList(),
                result.Start,
                result.Size,
                result.TotalSize
            );
        }
        
        [HttpGet]
        [Route("client-trading-conditions/all")]
        public async Task<IEnumerable<ClientTradingConditionsContract>> GetAllClientTradingConditions()
        {
            var result = await _accountManagementService.GetAllClients();

            return result.Select(x => _convertService.Convert<IClient, ClientTradingConditionsContract>(x)).ToList();
        }

        /// <summary>
        /// Update trading condition for client across all accounts
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        [HttpPatch]
        [Route("client-trading-conditions")]
        public async Task<ErrorCodeResponse<TradingConditionErrorCodesContract>> UpdateClientTradingConditions([FromBody] UpdateClientTradingConditionRequest request)
        {
            var result = await _accountManagementService.UpdateClientTradingCondition(request.ClientId,
                request.TradingConditionId,
                request.Username);

            var response = new ErrorCodeResponse<TradingConditionErrorCodesContract>();
            if (result.IsFailed)
            {
                response.ErrorCode =
                    _convertService.Convert<TradingConditionErrorCodes, TradingConditionErrorCodesContract>(result.Error.Value);
            }
            

            return response;
        }

        [HttpPatch]
        [Route("client-trading-conditions/bulk")]
        public async Task<ErrorCodeResponse<TradingConditionErrorCodesContract>> UpdateClientTradingConditions(
            [FromBody] UpdateClientTradingConditionsBulkRequest request)
        {
            var updates = request.Updates.Select(p => (p.ClientId, p.TradingConditionId)).ToList();
            var result = await _accountManagementService.UpdateClientTradingConditions(updates, 
                request.Username);

            var response = new ErrorCodeResponse<TradingConditionErrorCodesContract>();
            if (result.IsFailed)
            {
                response.ErrorCode =
                    _convertService.Convert<TradingConditionErrorCodes, TradingConditionErrorCodesContract>(result.Error.Value);
            }

            return response;
        }

        /// <summary>
        /// Creates an account
        /// </summary>
        [HttpPost]
        [Route("{clientId}")]
        [Obsolete("Use a single-parameter Create.")]
        public Task<IActionResult> Create([NotNull] string clientId,
            [FromBody][NotNull] CreateAccountRequestObsolete request)
        {
            return Create(new CreateAccountRequest
            {
                ClientId = clientId,
                AccountId = request.AccountId,
                TradingConditionId = request.TradingConditionId,
                BaseAssetId = request.BaseAssetId,
            });
        }

        /// <summary>
        /// Creates an account
        /// </summary>
        [HttpPost]
        [Route("")]
        public async Task<IActionResult> Create([FromBody][NotNull] CreateAccountRequest request)
        {
            try
            {
                var account = await Convert(
                    _accountManagementService.CreateAsync(
                        request.ClientId.RequiredNotNullOrWhiteSpace(nameof(request.ClientId)),
                        request.AccountId.RequiredNotNullOrWhiteSpace(nameof(request.AccountId)),
                        request.TradingConditionId,
                        request.BaseAssetId.RequiredNotNullOrWhiteSpace(nameof(request.BaseAssetId)),
                        request.AccountName,
                        request.UserId));
                
                return StatusCode((int) HttpStatusCode.Created, account);
            }
            catch (NotSupportedException e)
            {
                _logger.WriteError("Account creation", request.ToJson(), e);
                return Conflict();
            }
        }

        /// <summary>
        /// Changes an account.
        /// </summary>
        [HttpPatch]
        [Route("{clientId}/{accountId}")]
        [Obsolete("Use a two-parameter Change.")]
        public async Task<AccountContract> Change([NotNull] string clientId, [NotNull] string accountId,
            [FromBody] [NotNull] ChangeAccountRequest request)
        {
            return await Change(accountId, request);
        }

        /// <summary>
        /// Changes an account.
        /// </summary>
        [HttpPatch]
        [Route("{accountId}")]
        public async Task<AccountContract> Change([NotNull] string accountId,
            [FromBody] [NotNull] ChangeAccountRequest request)
        {
            if (request.IsDisabled == null &&
                request.IsWithdrawalDisabled == null)
            {
                return await GetById(accountId);
            }

            try
            {
                var result = await _accountManagementService.UpdateAccountAsync(accountId,
                    request.IsDisabled,
                    request.IsWithdrawalDisabled);

                return Convert(result);
            }
            catch (DisableAccountWithPositionsOrOrdersException)
            {
                throw new Exception(AccountsApiErrorCodes.DisableAccountWithPositionsOrOrders);
            }
        }

        /// <summary>
        /// Delete accounts. For TEST purposes only!
        /// </summary>
        [HttpPost("delete")]
        public Task Delete([Body] List<string> accountIds)
        {
            accountIds.RequiredNotNullOrEmpty(nameof(accountIds), $"{nameof(accountIds)} must be set.");

            _cqrsSender.SendCommandToSelf(new DeleteAccountsCommand
            {
                OperationId = Guid.NewGuid().ToString("N"),
                Timestamp = _systemClock.UtcNow.UtcDateTime,
                AccountIds = accountIds,
                Comment = "Started from API for test purposes.",
            });
            
            return Task.CompletedTask;
        }

        #endregion CRUD

        #region Deposit/Withdraw/ChargeManually

        /// <summary>
        /// Starts the operation of manually charging the client's account.
        /// Amount is absolute, i.e. negative value goes for charging.
        /// </summary>
        [HttpPost]
        [Route("{clientId}/{accountId}/balance")]
        [Obsolete("Use a two-parameter BeginChargeManually.")]
        public async Task<string> BeginChargeManually([NotNull] string clientId, [NotNull] string accountId,
            [FromBody][NotNull] AccountChargeManuallyRequest request)
        {
            return await BeginChargeManually(accountId, request);
        }

        /// <summary>
        /// Starts the operation of manually charging the client's account.
        /// Amount is absolute, i.e. negative value goes for charging.
        /// </summary>
        [HttpPost]
        [Route("{accountId}/balance")]
        public async Task<string> BeginChargeManually([NotNull] string accountId,
            [FromBody][NotNull] AccountChargeManuallyRequest request)
        {
            var account = await _accountManagementService.EnsureAccountValidAsync(accountId);

            var amount = await _accuracyService.ToAccountAccuracy(
                request.AmountDelta.RequiredNotEqualsTo(default, nameof(request.AmountDelta)),
                account.BaseAssetId, nameof(BeginChargeManually));

            return await _sendBalanceCommandsService.ChargeManuallyAsync(
                accountId.RequiredNotNullOrWhiteSpace(nameof(accountId)),
                amount,
                request.OperationId.RequiredNotNullOrWhiteSpace(nameof(request.OperationId)),
                request.Reason,
                "Api",
                request.AdditionalInfo,
                request.ReasonType.ToType<AccountBalanceChangeReasonType>(),
                request.EventSourceId,
                request.AssetPairId,
                request.TradingDay ?? _systemClock.UtcNow.DateTime);
        }

        /// <summary>
        /// Starts the operation of depositing funds to the client's account. Amount should be positive.
        /// </summary>
        [HttpPost]
        [Route("{accountId}/balance/deposit")]
        public async Task<string> BeginDeposit([NotNull] string accountId,
            [FromBody][NotNull] AccountChargeRequest request)
        {
            var account = await _accountManagementService.EnsureAccountValidAsync(accountId);

            var amount = await _accuracyService.ToAccountAccuracy(
                request.AmountDelta.RequiredGreaterThan(default, nameof(request.AmountDelta)),
                account.BaseAssetId, nameof(BeginDeposit));

            return await _sendBalanceCommandsService.DepositAsync(
                accountId.RequiredNotNullOrWhiteSpace(nameof(accountId)),
                amount,
                request.OperationId.RequiredNotNullOrWhiteSpace(nameof(request.OperationId)),
                request.Reason,
                request.AdditionalInfo);
        }

        /// <summary>
        /// Starts the operation of withdrawing funds to the client's account. Amount should be positive.
        /// </summary>
        [HttpPost]
        [Route("{accountId}/balance/withdraw")]
        public async Task<string> BeginWithdraw([NotNull] string accountId,
            [FromBody][NotNull] AccountChargeRequest request)
        {
            var result = await TryBeginWithdraw(accountId, request);

            if (result.Error != WithdrawalErrorContract.None)
            {
                throw new Exception($"Error: {result.Error.ToString()}. Details: {result.ErrorDetails}");
            }

            return result.OperationId;
        }

        /// <summary>
        /// Tries to start the operation of withdrawing funds to the client's account. Amount should be positive.
        /// </summary>
        [HttpPost]
        [Route("~/api/v2/accounts/{accountId}/balance/withdraw")]
        public async Task<WithdrawalResponse> TryBeginWithdraw([NotNull] string accountId,
            [FromBody][NotNull] AccountChargeRequest request)
        {
            if (string.IsNullOrEmpty(request.OperationId))
            {
                return new WithdrawalResponse
                {
                    Amount = request.AmountDelta,
                    Error = WithdrawalErrorContract.InvalidRequest,
                    ErrorDetails = "OperationID is missing"
                };
            }

            IAccount account;

            try
            {
                account = await _accountManagementService.EnsureAccountValidAsync(accountId);
            }
            catch (Exception e)
            {
                return new WithdrawalResponse
                {
                    Amount = request.AmountDelta,
                    Error = WithdrawalErrorContract.InvalidAccount,
                    ErrorDetails = e.Message
                };
            }

            if (account.IsWithdrawalDisabled)
            {
                return new WithdrawalResponse
                {
                    Amount = request.AmountDelta,
                    Error = WithdrawalErrorContract.WithdrawalDisabled
                };
            }

            var amount = await _accuracyService.ToAccountAccuracy(
                request.AmountDelta,
                account.BaseAssetId,
                nameof(BeginWithdraw));

            if (amount <= 0)
            {
                return new WithdrawalResponse
                {
                    Amount = amount,
                    Error = WithdrawalErrorContract.InvalidAmount
                };
            }
            
            var brokerSettingsResponse = await _brokerSettingsApi.GetByIdAsync(_brokerConfigurationAccessor.BrokerId);
            if (brokerSettingsResponse.ErrorCode != BrokerSettingsErrorCodesContract.None)
            {
                throw new InvalidOperationException($"Cannot read broker settings for {_brokerConfigurationAccessor.BrokerId}, " +
                                                    $"because of {brokerSettingsResponse.ErrorCode}");
            }

            switch (brokerSettingsResponse.BrokerSettings.WithdrawalMode)
            {
                case WithdrawalMode.DuringTradingHours:
                    {
                        var platformInfo = await _scheduleSettingsApi.GetPlatformInfo();

                        if (!platformInfo.IsTradingEnabled)
                        {
                            return new WithdrawalResponse
                            {
                                Amount = amount,
                                Error = WithdrawalErrorContract.OutOfTradingHours,
                                ErrorDetails = $"Platform is our of trading hours " +
                                               $"Last trading day: {platformInfo.LastTradingDay}," +
                                               $" next will start: {platformInfo.NextTradingDayStart}"
                            };
                        }
                        break;
                    }
                case WithdrawalMode.BusinessDays:
                    {
                        var platformInfo = await _scheduleSettingsApi.GetPlatformInfo();

                        if (!platformInfo.IsBusinessDay)
                        {
                            return new WithdrawalResponse
                            {
                                Amount = amount,
                                Error = WithdrawalErrorContract.OutOfBusinessDays
                            };
                        }
                        break;
                    }
                case WithdrawalMode.Always:
                    break;
                default:
                    throw new ArgumentException($"Unknown switch: {brokerSettingsResponse.BrokerSettings.WithdrawalMode}",
                        nameof(brokerSettingsResponse.BrokerSettings.WithdrawalMode));
            }

            try
            {
                var operationId = await _sendBalanceCommandsService.WithdrawAsync(
                    accountId,
                    amount,
                    request.OperationId,
                    request.Reason,
                    request.AdditionalInfo);

                return new WithdrawalResponse
                {
                    Amount = amount,
                    OperationId = operationId,
                    Error = WithdrawalErrorContract.None
                };
            }
            catch (Exception e)
            {
                return new WithdrawalResponse
                {
                    Amount = amount,
                    Error = WithdrawalErrorContract.UnknownError,
                    ErrorDetails = e.Message
                };
            }
        }

        #endregion Deposit/Withdraw/ChargeManually

        #region System actions

        /// <summary>
        /// Reset account balance to default value (from settings)
        /// </summary>
        /// <returns></returns>
        [HttpPost]
        [Route("{accountId}/reset")]
        public async Task Reset([NotNull] string accountId)
        {
            await _accountManagementService.ResetAccountAsync(accountId.RequiredNotNullOrWhiteSpace(nameof(accountId)));
        }

        /// <summary>
        /// Creates default accounts for client by trading condition id.
        /// </summary>
        [HttpPost]
        [Route("default-accounts")]
        public Task<List<AccountContract>> CreateDefaultAccounts([NotNull] CreateDefaultAccountsRequest request)
        {
            return Convert(_accountManagementService.CreateDefaultAccountsAsync(
                request.ClientId.RequiredNotNullOrWhiteSpace(nameof(request.ClientId)),
                request.TradingConditionId.RequiredNotNullOrWhiteSpace(nameof(request.TradingConditionId))));
        }

        /// <summary>
        /// Create accounts with requested base asset for all users 
        /// that already have accounts with requested trading condition
        /// </summary>
        [HttpPost]
        [Route("new-base-asset")]
        public Task<List<AccountContract>> CreateAccountsForNewBaseAsset([NotNull] CreateAccountsForBaseAssetRequest request)
        {
            var account = _accountManagementService.CreateAccountsForNewBaseAssetAsync(
                request.TradingConditionId.RequiredNotNullOrWhiteSpace(nameof(request.TradingConditionId)),
                request.BaseAssetId.RequiredNotNullOrWhiteSpace(nameof(request.BaseAssetId)));
            return Convert(account);
        }



        #endregion System actions

        /// <summary>
        /// Get account statistics for the current trading day
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpGet("stat/{accountId}")]
        public async Task<AccountStatContract> GetStat(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new ArgumentNullException(nameof(accountId), "Account must be set.");
            }

            var stat = await _accountManagementService.GetCachedAccountStatistics(accountId);
            
            return stat != null ? _convertService.Convert<AccountStat, AccountStatContract>(stat) : null;
        }
        
        /// <summary>
        /// Recalculates account statistics
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        [HttpPost("stat/{accountId}/recalculate")]
        public async Task<IActionResult> RecalculateStat(string accountId)
        {
            if (string.IsNullOrWhiteSpace(accountId))
            {
                throw new ArgumentNullException(nameof(accountId), "Account must be set.");
            }

            await _accountManagementService.ClearStatsCache(accountId);

            return Ok();
        }

        private async Task<Contracts.PaginatedResponseContract<AccountContract>> Convert(Task<PaginatedResponse<IAccount>> accounts)
        {
            var data = await accounts;
            return new Contracts.PaginatedResponseContract<AccountContract>(
                data.Contents.Select(Convert).ToList(),
                data.Start,
                data.Size,
                data.TotalSize
            );
        }

        private async Task<List<AccountContract>> Convert(Task<IReadOnlyList<IAccount>> accounts)
        {
            return (await accounts).Select(Convert).OrderBy(a => a.ClientId).ThenBy(a => a.BaseAssetId)
                .ThenBy(a => a.Id).ToList();
        }

        private async Task<AccountContract> Convert(Task<IAccount> accountTask)
        {
            return Convert(await accountTask);
        }

        private AccountContract Convert(IAccount account)
        {
            return _convertService.Convert<IAccount, AccountContract>(account);
        }

        private AccountSuggestedContract Convert(IAccountSuggested item)
        {
            return _convertService.Convert<IAccountSuggested, AccountSuggestedContract>(item);
        }
    }
}