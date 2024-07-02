// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using Autofac;

using BookKeeper.Client.Workflow.Events;

using Lykke.Cqrs;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Snow.Mdm.Contracts.Models.Events;

using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.Backend.Contracts.Events;

using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Services.Implementation
{
    public class StartupManager : IStartupManager
    {
        private readonly IAuditRepository _auditRepository;
        private readonly IEodTaxFileMissingRepository _taxFileMissingRepository;
        private readonly IComplexityWarningRepository _complexityWarningRepository;
        private readonly IAccountsRepository _accountsRepository;
        private readonly IAccountHistoryRepository _accountHistoryRepository;
        private readonly IEnumerable<IStartable> _startables;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly IBrokerSettingsCache _brokerSettingsCache;
        private readonly IComplexityWarningConfiguration _complexityWarningConfiguration;
        private readonly RabbitMqListener<BrokerSettingsChangedEvent> _brokerSettingsListener;
        private readonly RabbitMqListener<EodProcessFinishedEvent> _eodFinishedListener;
        private readonly RabbitMqListener<OrderHistoryEvent> _orderHistoryListener;
        private readonly ILogger _logger;

        public StartupManager(
            IAuditRepository auditRepository,
            IEodTaxFileMissingRepository taxFileMissingRepository,
            IComplexityWarningRepository complexityWarningRepository,
            IAccountsRepository accountsRepository,
            IAccountHistoryRepository accountHistoryRepository,
            ILogger<StartupManager> logger,
            IEnumerable<IStartable> startables,
            ICqrsEngine cqrsEngine,
            IBrokerSettingsCache brokerSettingsCache,
            IComplexityWarningConfiguration complexityWarningConfiguration,
            RabbitMqListener<BrokerSettingsChangedEvent> brokerSettingsListener,
            RabbitMqListener<EodProcessFinishedEvent> eodFinishedListener,
            RabbitMqListener<OrderHistoryEvent> orderHistoryListener)
        {
            _auditRepository = auditRepository;
            _taxFileMissingRepository = taxFileMissingRepository;
            _complexityWarningRepository = complexityWarningRepository;
            _accountsRepository = accountsRepository;
            _accountHistoryRepository = accountHistoryRepository;
            _startables = startables;
            _cqrsEngine = cqrsEngine;
            _brokerSettingsCache = brokerSettingsCache;
            _complexityWarningConfiguration = complexityWarningConfiguration;
            _brokerSettingsListener = brokerSettingsListener;
            _eodFinishedListener = eodFinishedListener;
            _orderHistoryListener = orderHistoryListener;
            _logger = logger;
        }

        public void Start()
        {
            _complexityWarningConfiguration.Validate().GetAwaiter().GetResult();

            _logger.LogInformation("Initializing broker settings cache");
            _brokerSettingsCache.Initialize();
            _logger.LogInformation("Broker settings cache initialized");

            _brokerSettingsListener.Start();
            _logger.LogInformation("Broker settings listener started");
            
            _eodFinishedListener.Start();
            _logger.LogInformation("EOD listener started");
            
            _orderHistoryListener.Start();
            _logger.LogInformation("Order history listener started");

            // start publishers
            foreach (var component in this._startables)
            {
                var cName = component.GetType().Name;

                try
                {
                    component.Start();

                    _logger.LogInformation($"Started {cName} successfully.");
                }
                catch (Exception e)
                {
                    _logger.LogError(e, $"Couldn't start component {cName}.");
                    throw;
                }
            }

            _logger.LogInformation("Initializing repositories");
            _auditRepository.Initialize();
            _taxFileMissingRepository.Initialize();
            _complexityWarningRepository.Initialize();
            _accountsRepository.Initialize();
            _accountHistoryRepository.Initialize();
            _logger.LogInformation("Repositories initialization done");


            _logger.LogInformation("Initializing CQRS engine");
            _cqrsEngine.StartAll();
            _logger.LogInformation("CQRS engine initialized");
        }
    }
}