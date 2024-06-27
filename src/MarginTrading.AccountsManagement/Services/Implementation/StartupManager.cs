// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

using Autofac;

using Lykke.Cqrs;

using MarginTrading.AccountsManagement.Repositories;
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
        private readonly ILogger _logger;
        private readonly IEnumerable<IStartable> _startables;
        private readonly ICqrsEngine _cqrsEngine;
        private readonly IBrokerSettingsCache _brokerSettingsCache;

        public StartupManager(IAuditRepository auditRepository,
            IEodTaxFileMissingRepository taxFileMissingRepository,
            IComplexityWarningRepository complexityWarningRepository,
            IAccountsRepository accountsRepository,
            IAccountHistoryRepository accountHistoryRepository,
            ILogger<StartupManager> logger,
            IEnumerable<IStartable> startables,
            ICqrsEngine cqrsEngine,
            IBrokerSettingsCache brokerSettingsCache)
        {
            _auditRepository = auditRepository;
            _taxFileMissingRepository = taxFileMissingRepository;
            _complexityWarningRepository = complexityWarningRepository;
            _accountsRepository = accountsRepository;
            _accountHistoryRepository = accountHistoryRepository;
            _logger = logger;
            _startables = startables;
            _cqrsEngine = cqrsEngine;
            _brokerSettingsCache = brokerSettingsCache;
        }

        public void Start()
        {
            _logger.LogInformation("Initializing broker settings cache");
            _brokerSettingsCache.Initialize();
            _logger.LogInformation("Broker settings cache initialized");
            
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