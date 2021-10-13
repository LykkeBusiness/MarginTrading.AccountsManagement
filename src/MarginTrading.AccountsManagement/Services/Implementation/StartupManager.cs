// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Common.Log;
using MarginTrading.AccountsManagement.Repositories;

namespace MarginTrading.AccountsManagement.Services.Implementation
{
    public class StartupManager : IStartupManager
    {
        private readonly IAuditRepository _auditRepository;
        private readonly IEodTaxFileMissingRepository _taxFileMissingRepository;
        private readonly IComplexityWarningRepository _complexityWarningRepository;
        private readonly IAccountsRepository _accountsRepository;
        
        private readonly ILog _log;

        public StartupManager(ILog log, IAuditRepository auditRepository, IEodTaxFileMissingRepository taxFileMissingRepository, IComplexityWarningRepository complexityWarningRepository, IAccountsRepository accountsRepository)
        {
            _log = log;
            _auditRepository = auditRepository;
            _taxFileMissingRepository = taxFileMissingRepository;
            _complexityWarningRepository = complexityWarningRepository;
            _accountsRepository = accountsRepository;
        }

        public async Task StartAsync()
        {
            _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "Initializing repositories.");
            
            _auditRepository.Initialize();
            
            _taxFileMissingRepository.Initialize();
            
            _complexityWarningRepository.Initialize();
            
            _accountsRepository.Initialize();

            _log.WriteInfoAsync(nameof(StartupManager), nameof(StartAsync), "Repositories initialization done.");

            await Task.CompletedTask;
        }
    }
}