// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading;

using Autofac;

using BookKeeper.Client.Workflow.Events;

using Lykke.Cqrs;
using Lykke.RabbitMqBroker.Publisher;
using Lykke.RabbitMqBroker.Subscriber;
using Lykke.Snow.Mdm.Contracts.Models.Events;

using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.Backend.Contracts.Events;

using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Services.Implementation;

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
    private readonly IRabbitPublisher<AutoComputedLossPercentageUpdateEvent> _lossPercentageProducer;
    private readonly RabbitMqListener<BrokerSettingsChangedEvent> _brokerSettingsListener;
    private readonly RabbitMqListener<EodProcessFinishedEvent> _eodFinishedListener;
    private readonly RabbitMqListener<OrderHistoryEvent> _orderHistoryListener;
    private readonly ILogger _logger;
    private readonly CancellationTokenSource _cts = new();

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
        IRabbitPublisher<AutoComputedLossPercentageUpdateEvent> lossPercentageProducer,
        RabbitMqListener<BrokerSettingsChangedEvent> brokerSettingsListener = null,
        RabbitMqListener<EodProcessFinishedEvent> eodFinishedListener = null,
        RabbitMqListener<OrderHistoryEvent> orderHistoryListener = null)
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
        _lossPercentageProducer = lossPercentageProducer;
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

        if (_brokerSettingsListener != null)
        {
            _brokerSettingsListener.StartAsync(_cts.Token);
            _logger.LogInformation("Broker settings listener started");
        }

        if (_eodFinishedListener != null)
        {
            _eodFinishedListener.StartAsync(_cts.Token);
            _logger.LogInformation("EOD listener started");
        }

        if (_orderHistoryListener != null)
        {
            _orderHistoryListener.StartAsync(_cts.Token);
            _logger.LogInformation("Order history listener started");
        }

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

    public void Stop()
    {
        _cts.Cancel();

        if (_brokerSettingsListener != null)
        {
            _brokerSettingsListener.Stop();
            _logger.LogInformation("Broker settings listener stopped");
        }

        if (_eodFinishedListener != null)
        {
            _eodFinishedListener.Stop();
            _logger.LogInformation("EOD listener stopped");
        }

        if (_orderHistoryListener != null)
        {
            _orderHistoryListener.Stop();
            _logger.LogInformation("Order history listener stopped");
        }

        _lossPercentageProducer.Stop();
    }
}