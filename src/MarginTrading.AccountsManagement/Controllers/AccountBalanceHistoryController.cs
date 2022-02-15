// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.AccountsManagement.Extensions;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Controllers
{
    /// <inheritdoc cref="IAccountBalanceHistoryApi" />
    [Authorize]
    [Route("api/balance-history")]
    public class AccountBalanceHistoryController : Controller, IAccountBalanceHistoryApi
    {
        private readonly IAccountBalanceChangesRepository _accountBalanceChangesRepository;
        private readonly ISystemClock _systemClock;

        public AccountBalanceHistoryController(
            IAccountBalanceChangesRepository accountBalanceChangesRepository,
            ISystemClock systemClock)
        {
            _accountBalanceChangesRepository = accountBalanceChangesRepository;
            _systemClock = systemClock;
        }

        /// <summary>
        /// Get account balance change history paginated, by account Id, and optionally by dates and asset pair
        /// </summary>
        [Route("by-pages/{accountId}")]
        [HttpGet]
        public async Task<PaginatedResponseContract<AccountBalanceChangeContract>> ByPages(
            string accountId,
            [FromQuery] DateTime? @from = null, 
            [FromQuery] DateTime? to = null,
            [FromQuery] AccountBalanceChangeReasonTypeContract[] reasonTypes = null,
            [FromQuery] string assetPairId = null,
            [FromQuery] int? skip = null,
            [FromQuery] int? take = null,
            [FromQuery] bool isAscendingOrder = true)
        {
            var data = await _accountBalanceChangesRepository.GetByPagesAsync(
                accountId, 
                @from?.AssumeUtcIfUnspecified(),
                to?.AssumeUtcIfUnspecified(),
                reasonTypes?.Select(x => x.ToType<AccountBalanceChangeReasonType>()).ToArray(),
                assetPairId,
                skip,
                take,
                isAscendingOrder);
            
            return this.Convert(data);
        }

        /// <summary>
        /// Get account balance change history by account Id, and optionally by dates
        /// </summary>
        [Route("by-account/{accountId}")]
        [HttpGet]
        public async Task<Dictionary<string, AccountBalanceChangeContract[]>> ByAccount(
            string accountId,
            [FromQuery] DateTime? @from = null, 
            [FromQuery] DateTime? to = null,
            [FromQuery] AccountBalanceChangeReasonTypeContract? reasonType = null,
            [FromQuery] bool filterByTradingDay = false)
        {
            var data = await _accountBalanceChangesRepository.GetAsync(
                accountId, 
                @from?.AssumeUtcIfUnspecified(),
                to?.AssumeUtcIfUnspecified(),
                reasonType?.ToType<AccountBalanceChangeReasonType>(), 
                filterByTradingDay);
            
            return data.GroupBy(i => i.AccountId).ToDictionary(g => g.Key, g => g.Select(Convert).ToArray());
        }

        /// <summary>
        /// Get account balance change history by account Id and eventSourceId (like Withdraw or Deposit)
        /// </summary>
        [Route("{accountId}")]
        [HttpGet]
        public async Task<AccountBalanceChangeContract[]> ByAccountAndEventSource(
            string accountId, 
            [FromQuery] string eventSourceId = null)
        {
            var data = await _accountBalanceChangesRepository.GetAsync(accountId, eventSourceId);

            return data.Select(Convert).ToArray();
        }

        /// <summary>
        /// Get account balance on a particular date
        /// </summary>
        [Route("~/api/balance/{accountId}")]
        [HttpGet]
        public async Task<decimal> GetBalanceOnDate([FromRoute] string accountId, [FromQuery] DateTime? date)
        {
            var targetDate = date ?? _systemClock.UtcNow.UtcDateTime.Date;
            
            return await _accountBalanceChangesRepository.GetBalanceAsync(accountId, targetDate);
        }

        private AccountBalanceChangeContract Convert(IAccountBalanceChange arg)
        {
            return new AccountBalanceChangeContract(arg.Id, arg.ChangeTimestamp, arg.AccountId, arg.ClientId,
                arg.ChangeAmount, arg.Balance, arg.WithdrawTransferLimit, arg.Comment, 
                Enum.Parse<AccountBalanceChangeReasonTypeContract>(arg.ReasonType.ToString()),
                arg.EventSourceId, arg.LegalEntity, arg.AuditLog, arg.Instrument, arg.TradingDate);
        }

        private PaginatedResponseContract<AccountBalanceChangeContract> Convert(PaginatedResponse<IAccountBalanceChange> data)
        {
            return new PaginatedResponseContract<AccountBalanceChangeContract>(
                data.Contents.Select(Convert).ToList(),
                data.Start,
                data.Size,
                data.TotalSize
            );
        }
    }
}