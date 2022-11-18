using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Workflow.BrokerSettings;
using Microsoft.Extensions.DependencyInjection;

namespace MarginTrading.AccountsManagement.Workflow.ProductComplexity
{
    internal static class ServiceCollectionExtensions
    {
        public static void AddHostedServices(this IServiceCollection services, AppSettings settings)
        {
            services.AddHostedService<CleanupExpiredComplexityService>();
            services.AddHostedService<OrderHistoryListener>();
            services.AddHostedService(x => ActivatorUtilities.CreateInstance<BrokerSettingsListener>(
                x,
                settings.MarginTradingAccountManagement.BrokerId));
            if (settings.MarginTradingAccountManagement.LossPercentageCalculationEnabled)
            {
                services.AddHostedService(x => ActivatorUtilities.CreateInstance<EodProcessFinishedListener>(
                    x,
                    settings.MarginTradingAccountManagement.BrokerId));   
            } 
        }
    }
}
