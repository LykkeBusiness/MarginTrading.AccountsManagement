// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AzureStorage;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Repositories.AzureServices;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.AzureStorage
{
    internal class AccountBalanceChangesRepository : IAccountBalanceChangesRepository
    {
        private readonly IConvertService _convertService;
        private readonly ISystemClock _systemClock;
        private readonly INoSQLTableStorage<AccountBalanceChangeEntity> _tableStorage;

        public AccountBalanceChangesRepository(IReloadingManager<AccountManagementSettings> settings,
            ILog log,
            IConvertService convertService,
            ISystemClock systemClock,
            IAzureTableStorageFactoryService azureTableStorageFactoryService)
        {
            _convertService = convertService;
            _systemClock = systemClock;
            _tableStorage =
                azureTableStorageFactoryService.Create<AccountBalanceChangeEntity>(
                    settings.Nested(s => s.Db.ConnectionString), "AccountHistory", log);
        }

        public Task<(PaginatedResponse<IAccountBalanceChange> paginatedResponse, decimal totalAmount)> GetByPagesAsync(string accountId,
            DateTime? @from = null, DateTime? to = null, AccountBalanceChangeReasonType[] reasonTypes = null,
            string assetPairId = null, int? skip = null, int? take = null, bool isAscendingOrder = true)
        {
            throw new NotImplementedException();
        }

        public Task<IReadOnlyList<IAccountBalanceChangeLight>> GetLightAsync(DateTime? @from = null, DateTime? to = null)
        {
            throw new NotImplementedException();
        }

        public async Task<IReadOnlyList<IAccountBalanceChange>> GetAsync(string accountId, DateTime? @from = null,
            DateTime? to = null, AccountBalanceChangeReasonType? reasonType = null, bool filterByTradingDay = false)
        {
            return (await _tableStorage.WhereAsync(accountId, from ?? DateTime.MinValue,
                    to?.Date.AddDays(1) ?? DateTime.MaxValue, ToIntervalOption.IncludeTo,
                    x => reasonType == null || x.ReasonType == reasonType.ToString()))
                .OrderByDescending(item => item.ChangeTimestamp).ToList();
        }

        public async Task<IReadOnlyList<IAccountBalanceChange>> GetAsync(string accountId, string eventSourceId)
        {
            return (await _tableStorage.GetDataAsync(x => x.PartitionKey == accountId
                                                          && (string.IsNullOrWhiteSpace(eventSourceId) ||
                                                              x.EventSourceId == eventSourceId)))
                .OrderByDescending(item => item.ChangeTimestamp).ToList();
        }

        public Task<decimal> GetCompensationsProfit(string accountId, DateTime[] days)
        {
            throw new NotImplementedException();
        }

        public Task<decimal> GetDividendsProfit(string accountId, DateTime[] days)
        {
            throw new NotImplementedException();
        }

        public async Task<decimal> GetRealizedPnlAndCompensationsForToday(string accountId)
        {
            return (await _tableStorage.WhereAsync(accountId,
                    //TODO rethink the way trading day's start & end are selected 
                    _systemClock.UtcNow.UtcDateTime.Date,
                    DateTime.MaxValue,
                    ToIntervalOption.IncludeTo,
                    x => x.ReasonType == AccountBalanceChangeReasonType.RealizedPnL.ToString() ||
                         x.ReasonType == AccountBalanceChangeReasonType.CompensationPayments.ToString()))
                .Sum(x => x.ChangeAmount);
        }
        
        public async Task AddAsync(IAccountBalanceChange change)
        {
            var entity = _convertService.Convert<AccountBalanceChangeEntity>(change);
            // ReSharper disable once RedundantArgumentDefaultValue
            await _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(entity, entity.ChangeTimestamp,
                RowKeyDateTimeFormat.Iso);
        }

        public async Task<decimal> GetBalanceAsync(string accountId, DateTime date)
        {
            return (await _tableStorage.WhereAsync(accountId, DateTime.MinValue,
                       date.Date.AddDays(1), ToIntervalOption.ExcludeTo))
                   .OrderByDescending(item => item.ChangeTimestamp).FirstOrDefault()?.Balance ?? 0;
        }

        private AccountBalanceChange Convert(AccountBalanceChangeEntity arg)
        {
            return _convertService.Convert<AccountBalanceChange>(arg);
        }
    }
}