﻿using System.Threading.Tasks;
using AzureStorage;
using AzureStorage.Tables;
using Common.Log;
using Lykke.SettingsReader;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Models;
using MarginTrading.AccountsManagement.BrokerBase.Services;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker.Repositories.AzureRepositories
{
    internal class AccountHistoryRepository : IAccountHistoryRepository
    {
        private readonly INoSQLTableStorage<AccountHistoryEntity> _tableStorage;
        private readonly IConvertService _convertService;

        public AccountHistoryRepository(IReloadingManager<Settings> settings, ILog log,
            IConvertService convertService)
        {
            _tableStorage = AzureTableStorage<AccountHistoryEntity>.Create(settings.Nested(s => s.Db.HistoryConnString),
                "AccountHistory", log);
            _convertService = convertService;
        }

        public Task InsertOrReplaceAsync(IAccountHistory obj)
        {
            var entity = _convertService.Convert<AccountHistoryEntity>(obj);
            return _tableStorage.InsertOrReplaceAsync(entity);
        }
    }
}
