// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Lykke.Snow.Mdm.Contracts.Models.Contracts;

namespace MarginTrading.AccountsManagement.Services
{
    public interface IBrokerSettingsCache
    {
        Task Initialize();
        BrokerSettingsContract Get();
        void Update(BrokerSettingsContract brokerSettings);
    }
}