// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.MarginTrading.BrokerBase;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker
{
    public class Program : WebAppProgramBase<Startup>
    {
        public static void Main(string[] args)
        {
            RunOnPort(5021);
        }
    }
}