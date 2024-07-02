// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using Lykke.Snow.Common.Startup;
using Lykke.Snow.Mdm.Contracts.Models.Contracts;

using MarginTrading.AccountsManagement.Services;

namespace MarginTrading.AccountsManagement.Tests.Fakes
{
    internal sealed class SomeBrokerSettingsCache : IBrokerSettingsCache
    {
        private BrokerSettingsContract _brokerSettings;
        
        public Task Initialize()
        {
            _brokerSettings = new BrokerSettingsContract{BrokerId = new BrokerId("some broker")};
            return Task.CompletedTask;
        }

        public BrokerSettingsContract Get()
        {
            return _brokerSettings;
        }

        public void Update(BrokerSettingsContract brokerSettings)
        {
            if (brokerSettings != null)
            {
                _brokerSettings = brokerSettings;
            }
        }
    }
}