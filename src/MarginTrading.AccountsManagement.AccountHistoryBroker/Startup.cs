// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Autofac;
using Lykke.HttpClientGenerator;
using Lykke.MarginTrading.BrokerBase;
using Lykke.MarginTrading.BrokerBase.Models;
using Lykke.MarginTrading.BrokerBase.Settings;
using Lykke.SettingsReader;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Models;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Repositories;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Services;
using MarginTrading.AccountsManagement.Contracts;
using Microsoft.Extensions.Configuration;
using SqlRepos = MarginTrading.AccountsManagement.AccountHistoryBroker.Repositories.SqlRepositories;
using Microsoft.Extensions.Hosting;
using Common;
using Microsoft.Extensions.Logging;
using Lykke.Snow.Common.Correlation.RabbitMq;
using Lykke.RabbitMqBroker;
using Lykke.RabbitMqBroker.Publisher.Serializers;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Publisher.Strategies;
using Common.Log;
using MarginTrading.AccountsManagement.Contracts.Commands;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker
{
    public class Startup : BrokerStartupBase<DefaultBrokerApplicationSettings<Settings>, Settings>
    {
        protected override string ApplicationName => "AccountHistoryBroker";

        public Startup(IHostEnvironment env, IConfiguration configuration) : base(env, configuration)
        {
        }

        protected override void RegisterCustomServices(ContainerBuilder builder, 
            IReloadingManager<Settings> settings)
        {
            builder.RegisterClient<IAccountsApi>(settings.CurrentValue.AccountManagement.ServiceUrl,
                config =>
                    config
                        .WithServiceName<LykkeErrorResponse>($"Account Management")
                        .WithOptionalApiKey(settings.CurrentValue.AccountManagement.ApiKey));

            builder.RegisterType<Application>().As<IBrokerApplication>().SingleInstance();
            builder.RegisterInstance(new ConvertService())
                .As<IConvertService>().SingleInstance();

            if (settings.CurrentValue.Db.StorageMode == StorageMode.SqlServer.ToString())
            {
                builder.RegisterType<SqlRepos.AccountHistoryRepository>()
                    .As<IAccountHistoryRepository>()
                    .SingleInstance();
            }
            else if (settings.CurrentValue.Db.StorageMode == StorageMode.Azure.ToString())
            {
                throw new InvalidOperationException("Azure storage mode is not supported");
            }

            builder.RegisterType<TaxHistoryInsertedPublisher>();
        }
        
        // TODO: remove
        // private static Lykke.RabbitMqBroker.Publisher.IMessageProducer<TMessage> GetProducer<TMessage>(
        //    ILoggerFactory loggerFactory,
        //    RabbitMqCorrelationManager correlationManager,
        //    RabbitMqSubscriptionSettings settings,
        //    IRabbitMqSerializer<TMessage> serializer, ILog logger, IRabbitMqPublishStrategy publishStrategy,
        //    IApplicationLifetime applicationLifetime,
        //    IPublishingQueueRepository publishingQueueRepository = null)
        // {
        //    var publisher = new RabbitMqPublisher<TMessage>(loggerFactory, settings);

        //    if (settings.IsDurable && publishingQueueRepository != null)
        //        publisher.SetQueueRepository(publishingQueueRepository);
        //    else
        //        publisher.DisableInMemoryQueuePersistence();
        //        
        //    applicationLifetime.ApplicationStopping.Register(obj => ((IDisposable)obj).Dispose(), publisher);

        //    var result = publisher
        //        .SetSerializer(serializer)
        //        .SetPublishStrategy(publishStrategy)
        //        .SetWriteHeadersFunc(correlationManager.BuildCorrelationHeadersIfExists);
        //    result.Start();
        //    return result;
        // }
    }
}