// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace MarginTrading.AccountsManagement.Services
{
    public interface IMeteorSender
    {
        Task Send871mWarningConfirmed(string accountId);
    }
}