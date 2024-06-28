// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using Lykke.Snow.Mdm.Contracts.BrokerFeatures;

using Microsoft.FeatureManagement;

using BrokerFeature = Lykke.Snow.Mdm.Contracts.BrokerFeatures.BrokerFeature;

namespace MarginTrading.AccountsManagement.Settings
{
    public sealed class ComplexityWarningConfiguration
    {
        private readonly AccountManagementSettings _settings;
        private readonly Lazy<Task<bool>> _featureEnabled;

        public ComplexityWarningConfiguration(
            IFeatureManager featureManager,
            AccountManagementSettings settings)
        {
            _settings = settings;
            _featureEnabled = new Lazy<Task<bool>>(
                () => featureManager.IsEnabledAsync(BrokerFeature.ProductComplexityWarning));
        }
        
        public Task<bool> IsEnabled => _featureEnabled.Value;

        public async Task Validate()
        {
            if (!await _featureEnabled.Value)
            {
                return;
            }

            if (_settings.ComplexityWarningsCount <= 0)
            {
                throw new InvalidOperationException(
                    $"Broker {_settings.BrokerId} feature {BrokerFeature.ProductComplexityWarning} is enabled, " +
                    $"but {nameof(_settings.ComplexityWarningsCount)} = {_settings.ComplexityWarningsCount} is not valid ");
            }
        }
    }
}