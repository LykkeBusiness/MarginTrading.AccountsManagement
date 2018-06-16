﻿using System;
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

namespace MarginTrading.AccountsManagement.Repositories.Implementation.AzureStorage
{
    internal class AccountBalanceChangesRepository : IAccountBalanceChangesRepository
    {
        private readonly IConvertService _convertService;
        private readonly INoSQLTableStorage<AccountBalanceChangeEntity> _tableStorage;

        public AccountBalanceChangesRepository(IReloadingManager<AccountManagementSettings> settings, ILog log,
            IConvertService convertService, IAzureTableStorageFactoryService azureTableStorageFactoryService)
        {
            _convertService = convertService;
            _tableStorage =
                azureTableStorageFactoryService.Create<AccountBalanceChangeEntity>(
                    settings.Nested(s => s.Db.ConnectionString), "AccountHistory", log);
        }

        public async Task<IReadOnlyList<IAccountBalanceChange>> GetAsync(string[] accountIds, DateTime? @from,
            DateTime? to)
        {
            return (await _tableStorage.WhereAsync(accountIds, from ?? DateTime.MinValue,
                    to?.Date.AddDays(1) ?? DateTime.MaxValue, ToIntervalOption.IncludeTo))
                .OrderByDescending(item => item.ChangeTimestamp).ToList();
        }

        public async Task AddAsync(IAccountBalanceChange change)
        {
            var entity = _convertService.Convert<AccountBalanceChangeEntity>(change);
            // ReSharper disable once RedundantArgumentDefaultValue
            await _tableStorage.InsertAndGenerateRowKeyAsDateTimeAsync(entity, entity.ChangeTimestamp,
                RowKeyDateTimeFormat.Iso);
        }

        private AccountBalanceChange Convert(AccountBalanceChangeEntity arg)
        {
            return _convertService.Convert<AccountBalanceChange>(arg);
        }
    }
}