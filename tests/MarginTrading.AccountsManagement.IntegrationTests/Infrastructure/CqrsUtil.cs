using System;
using System.Collections.Generic;

using Common.Log;

using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging;
using Lykke.Messaging.RabbitMq;
using Lykke.Messaging.Serialization;
using Lykke.Snow.Common.Startup;
using Lykke.Snow.Cqrs;

using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.IntegrationTests.Settings;

using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.IntegrationTests.Infrastructure
{
    public static class CqrsUtil
    {
        private static readonly RabbitMqCqrsEngine _cqrsEngine = CreateEngine();

        private static RabbitMqCqrsEngine CreateEngine()
        {
            var sett = SettingsUtil.Settings.MarginTradingAccountManagement.Cqrs;
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = new Uri(sett.ConnectionString, UriKind.Absolute)
            };

            var log = new LogToConsole();
            
            var rabbitMqConventionEndpointResolver =
                new RabbitMqConventionEndpointResolver("RabbitMq", SerializationFormat.MessagePack, environment: sett.EnvironmentName);
            
            return new RabbitMqCqrsEngine(
                log,
                new DependencyResolver(),
                new DefaultEndpointProvider(),
                rabbitMqSettings.Endpoint.ToString(),
                rabbitMqSettings.UserName,
                rabbitMqSettings.Password,
                true,
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