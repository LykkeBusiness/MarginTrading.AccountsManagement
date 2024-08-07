﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Autofac;

using Common.Log;

using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Messaging.Serialization;
using Lykke.Snow.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Settings;

using Moq;
using AutofacDependencyResolver = Lykke.Cqrs.AutofacDependencyResolver;

namespace MarginTrading.AccountsManagement.TestClient
{
    internal class CqrsFake
    {
        private const string DefaultRoute = "self";
        private const string DefaultPipeline = "commands";
        private readonly CqrsSettings _settings;
        private readonly long _defaultRetryDelayMs;
        private readonly CqrsContextNamesSettings _contextNames;

        public CqrsFake(CqrsSettings settings)
        {
            _settings = settings;
            _defaultRetryDelayMs = (long) _settings.RetryDelay.TotalMilliseconds;
            _contextNames = _settings.ContextNames;
        }
        
        public CqrsEngine CreateEngine()
        {
            var rabbitMqConventionEndpointResolver = new RabbitMqConventionEndpointResolver(
                "RabbitMq",
                SerializationFormat.MessagePack,
                environment: _settings.EnvironmentName);
            
            var rabbitMqSettings = new RabbitMQ.Client.ConnectionFactory
            {
                Uri = new Uri(_settings.ConnectionString, UriKind.Absolute)
            };

            return new RabbitMqCqrsEngine(
                EmptyLog.Instance,
                new AutofacDependencyResolver(Mock.Of<IComponentContext>()),
                new DefaultEndpointProvider(),
                rabbitMqSettings.Endpoint.ToString(),
                rabbitMqSettings.UserName,
                rabbitMqSettings.Password,
                false,
                Register.DefaultEndpointResolver(rabbitMqConventionEndpointResolver),
                RegisterContext());
        }

        private IRegistration RegisterContext()
        {
            var contextRegistration = Register.BoundedContext(_contextNames.AccountsManagement)
                .FailedCommandRetryDelay(_defaultRetryDelayMs)
                .ProcessingOptions(DefaultRoute).MultiThreaded(8).QueueCapacity(1024);
            contextRegistration
                .PublishingEvents(
                    typeof(AccountChangedEvent))
                .With(DefaultPipeline);
            return contextRegistration;
        }
    }
}