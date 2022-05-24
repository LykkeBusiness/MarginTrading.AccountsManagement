// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading;
using System.Threading.Tasks;
using Lykke.Snow.Mdm.Contracts.Api;
using Lykke.Snow.Mdm.Contracts.Models.Contracts;

namespace MarginTrading.AccountsManagement.Services.Implementation
{
    public class BrokerSettingsCache : IBrokerSettingsCache
    {
        private readonly ReaderWriterLockSlim _lockSlim = new ReaderWriterLockSlim();
        private readonly IBrokerSettingsApi _brokerSettingsApi;
        private readonly string _brokerId;

        private BrokerSettingsContract _brokerSettings;

        public BrokerSettingsCache(IBrokerSettingsApi brokerSettingsApi, string brokerId)
        {
            _brokerSettingsApi = brokerSettingsApi;
            _brokerId = brokerId;
        }

        public async Task Initialize()
        {
            var response = await _brokerSettingsApi.GetByIdAsync(_brokerId);

            if (response?.BrokerSettings == null || response.ErrorCode != BrokerSettingsErrorCodesContract.None)
                throw new Exception($"Missing broker settings for configured broker id:{_brokerId}");

            Update(response.BrokerSettings);
        }

        public BrokerSettingsContract Get()
        {
            _lockSlim.EnterReadLock();
            try
            {
                return _brokerSettings;
            }
            finally
            {
                _lockSlim.ExitReadLock();
            }
        }

        public void Update(BrokerSettingsContract brokerSettings)
        {
            _lockSlim.EnterWriteLock();
            try
            {
                _brokerSettings = brokerSettings;
            }
            finally
            {
                _lockSlim.ExitWriteLock();
            }
        } 
    }
}