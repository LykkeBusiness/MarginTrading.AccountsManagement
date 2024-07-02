using System.Threading;

using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.IntegrationTests.Infrastructure;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.IntegrationTests.WorkflowTests
{
    [SetUpFixture]
    public class InitNamespace
    {
        [OneTimeSetUp]
        public void RunBeforeAnyTests()
        {
            var sett = SettingsUtil.Settings.MarginTradingAccountManagement.Cqrs;
            var connectionString = sett.ConnectionString;
            var eventsExchange = $"{sett.EnvironmentName}.{sett.ContextNames.AccountsManagement}.events.exchange";
            
            RabbitUtil.ListenCqrsMessages<AccountChangedEvent>(connectionString, eventsExchange);
            RabbitUtil.ListenCqrsMessages<DepositSucceededEvent>(connectionString, eventsExchange);
            RabbitUtil.ListenCqrsMessages<WithdrawalSucceededEvent>(connectionString, eventsExchange);
            RabbitUtil.ListenCqrsMessages<WithdrawalFailedEvent>(connectionString, eventsExchange);

            // todo: register other messages
            
            // RabbitMqSubscriber does not wait for a reader thread to start before returning from the Start() method
            Thread.Sleep(500);
        }
    }
}