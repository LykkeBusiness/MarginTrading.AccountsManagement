// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Threading;
using AsyncFriendlyStackTrace;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.AccountsManagement.Settings;
using Newtonsoft.Json;
using Refit;

namespace MarginTrading.AccountsManagement.TestClient
{
    /// <summary>
    /// Simple way to check api clients are working.
    /// In future this could be turned into a functional testing app.
    /// </summary>
    internal static class Program
    {
        static void Main()
        {
            try
            {
                Run();
            }
            catch (ApiException e)
            {
                var str = e.Content;
                if (str.StartsWith('"'))
                {
                    str = TryDeserializeToString(str);
                }

                Console.WriteLine(str);
                Console.WriteLine(e.ToAsyncString());
            }
        }

        private static string TryDeserializeToString(string str)
        {
            try
            {
                return JsonConvert.DeserializeObject<string>(str);
            }
            catch
            {
                return str;
            }
        }

        private static void Run()
        {
            CheckBrokerRetries();
            Console.WriteLine("Successfuly finished");
        }

        private static void CheckBrokerRetries()
        {
            var cqrsEngine = new CqrsFake(new CqrsSettings
            {
                ConnectionString = "rabbit connstr here",
                ContextNames = new CqrsContextNamesSettings(),
                EnvironmentName = "andreev",
                RetryDelay = TimeSpan.FromSeconds(5),
            }).CreateEngine();
            
            Console.WriteLine("waiting 5 sec for cqrsEngine");
            Thread.Sleep(5000);

            cqrsEngine.PublishEvent(new AccountChangedEvent(
                DateTime.UtcNow, 
                "tetest1",
                new AccountContract(), 
                AccountChangedEventTypeContract.BalanceUpdated,
                new AccountBalanceChangeContract(
                    "tetetetest1",
                    DateTime.UtcNow, 
                    Enumerable.Repeat("t", 200).Aggregate((f, s) => $"{f}{s}"),//field has length of 64 
                    "tetest1",
                    1,
                    1,
                    10000,
                    "tetest1",
                    AccountBalanceChangeReasonTypeContract.Manual,
                    "tetest1",
                    "tetest1",
                    "tetest1",
                    "tetest1",
                    DateTime.MinValue
                ),
                null,
                null), new CqrsContextNamesSettings().AccountsManagement);
        }
    }
}