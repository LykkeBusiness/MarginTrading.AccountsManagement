// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

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
        private readonly ILogger _logger;

        public StartupManager(IAuditRepository auditRepository,
            IEodTaxFileMissingRepository taxFileMissingRepository,
            IComplexityWarningRepository complexityWarningRepository,
            IAccountsRepository accountsRepository,
            ILogger<StartupManager> logger)
        {
            _auditRepository = auditRepository;
            _taxFileMissingRepository = taxFileMissingRepository;
            _complexityWarningRepository = complexityWarningRepository;
            _accountsRepository = accountsRepository;
            _logger = logger;
        }

        public void Start()
        {
            _logger.LogInformation("Initializing repositories");
            
            _auditRepository.Initialize();
            _taxFileMissingRepository.Initialize();
            _complexityWarningRepository.Initialize();
            _accountsRepository.Initialize();

            _logger.LogInformation("Repositories initialization done");
        }
    }
}