﻿using Lykke.MarginTrading.BrokerBase;

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