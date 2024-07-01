using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Lykke.Snow.Mdm.Contracts.BrokerFeatures;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Services.Implementation;
using MarginTrading.AccountsManagement.Settings;
using Microsoft.Extensions.Hosting;
using Polly;

namespace MarginTrading.AccountsManagement.Workflow.ProductComplexity
{
    public class CleanupExpiredComplexityService : BackgroundService
    {
        private readonly ComplexityWarningConfiguration _complexityWarningConfiguration;
        private readonly ILogger _logger;
        private readonly AccountManagementSettings _settings;
        private readonly IAccountManagementService _accountManagementService;
        private readonly IComplexityWarningRepository _complexityWarningRepository;
        
        public CleanupExpiredComplexityService(
            ILogger<CleanupExpiredComplexityService> logger,
            AccountManagementSettings settings,
            IAccountManagementService accountManagementService,
            IComplexityWarningRepository complexityWarningRepository,
            ComplexityWarningConfiguration complexityWarningConfiguration)
        {
            _logger = logger;
            _settings = settings;
            _accountManagementService = accountManagementService;
            _complexityWarningRepository = complexityWarningRepository;
            _complexityWarningConfiguration = complexityWarningConfiguration;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!await _complexityWarningConfiguration.IsEnabled)
            {
                _logger.LogInformation("Feature {FeatureName} is disabled. {Action} will not be executed",
                    BrokerFeature.ProductComplexityWarning,
                    nameof(CleanupExpiredComplexityService) + '.' + nameof(Run));

                return;
            }

            _logger.LogInformation("Feature {FeatureName} is enabled. {Action} WILL BE executed",
                BrokerFeature.ProductComplexityWarning,
                nameof(CleanupExpiredComplexityService) + '.' + nameof(Run));

            var retryForever = Policy.Handle<Exception>()
                .RetryForeverAsync(onRetry: ex =>
                {
                    _logger.LogError(ex, "Error while executing {Action}", nameof(Run));
                });

            while (!stoppingToken.IsCancellationRequested)
            {
                await retryForever.ExecuteAsync(Run, stoppingToken);
                await Task.Delay(_settings.ComplexityWarningExpirationCheckPeriod, stoppingToken);
            }
        }

        private async Task Run(CancellationToken stoppingToken)
        {
            var expirationTimestamp = DateTime.UtcNow.Subtract(_settings.ComplexityWarningExpiration);

            var expiredAccounts = await _complexityWarningRepository.GetExpired(expirationTimestamp);

            foreach (var acc in expiredAccounts)
            {
                stoppingToken.ThrowIfCancellationRequested();

                _logger.LogInformation("Product complexity confirmation expired for account {AccountId}. " +
                                       "Complexity warning expiration : {ComplexityWarningExpirationValue}." +
                                       "Expiration timestamp : {ExpirationTimestamp}." +
                                       "Switched to False at : {SwitchedToFalseAt}." +
                                       "Resetting {FlagName} flag to true",
                    acc.AccountId,
                    _settings.ComplexityWarningExpiration,
                    expirationTimestamp,
                    acc.SwitchedToFalseAt,
                    nameof(IAccount.AdditionalInfo.ShouldShowProductComplexityWarning));
                
                acc.ResetConfirmation();
                await _accountManagementService.UpdateComplexityWarningFlag(acc.AccountId, shouldShowProductComplexityWarning: true);

                await _complexityWarningRepository.Save(acc);
            }
        }
    }
}
