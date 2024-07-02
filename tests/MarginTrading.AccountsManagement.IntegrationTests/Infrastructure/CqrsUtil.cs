using System;
using System.Collections.Generic;

using Common.Log;

using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;

using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.IntegrationTests.Settings;

namespace MarginTrading.AccountsManagement.IntegrationTests.Infrastructure
{
    public static class CqrsUtil
    {
        private static readonly CqrsEngine _cqrsEngine = CreateEngine();

        private static CqrsEngine CreateEngine()
        {
            var sett = SettingsUtil.Settings.MarginTradingAccountManagement.Cqrs;
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = sett.ConnectionString
            };

            var log = new LogToConsole();
            var messagingEngine = new MessagingEngine(log, new TransportResolver(new Dictionary<string, TransportInfo>
            {
                {
                    "RabbitMq",
                    new TransportInfo(rabbitMqSettings.Endpoint.ToString(), rabbitMqSettings.UserName,
                        rabbitMqSettings.Password, "None", "RabbitMq")
                }
            }), new RabbitMqTransportFactory());
            var rabbitMqConventionEndpointResolver =
                new RabbitMqConventionEndpointResolver("RabbitMq", "messagepack", environment: sett.EnvironmentName);
            return new CqrsEngine(log, new DependencyResolver(), messagingEngine, new DefaultEndpointProvider(), true,
                Register.DefaultEndpointResolver(rabbitMqConventionEndpointResolver),
                RegistrerBoundedContext(sett));
        }

        // todo: move to test-specific code
        private static IRegistration RegistrerBoundedContext(CqrsSettings sett)
        {
            return Register.BoundedContext(sett.ContextNames.TradingEngine)
                //todo place specific command here
                .PublishingCommands(typeof(DepositCommand))//BeginClosePositionBalanceUpdateCommand))
                .To(sett.ContextNames.AccountsManagement)
                .With("Default");
        }

        public static void SendCommandToAccountManagement<T>(T command)
        {
            var sett = SettingsUtil.Settings.MarginTradingAccountManagement.Cqrs;
            _cqrsEngine.SendCommand(command, sett.ContextNames.TradingEngine,
                sett.ContextNames.AccountsManagement);
        }

        private class DependencyResolver : IDependencyResolver
        {
            public object GetService(Type type)
            {
                return Activator.CreateInstance(type);
            }

            public bool HasService(Type type)
            {
                return !type.IsInterface;
            }
        }
    }
}