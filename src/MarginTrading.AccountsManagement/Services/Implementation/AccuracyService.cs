// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using MarginTrading.AssetService.Contracts;

namespace MarginTrading.AccountsManagement.Services.Implementation
{
    public class AccuracyService : IAccuracyService
    {
        private readonly IAssetsApi _assetsApi;
        private readonly ILogger _logger;

        private const int MaxAccuracy = 16;

        public AccuracyService(
            IAssetsApi assetsApi,
            ILogger<AccuracyService> logger)
        {
            _assetsApi = assetsApi;
            _logger = logger;
        }
        
        public async Task<decimal> ToAccountAccuracy(decimal amount, string accountBaseAsset, string operationName)
        {
            var asset = await _assetsApi.Get(accountBaseAsset);

            var accuracy = asset?.Accuracy ?? MaxAccuracy;

            var roundedValue = Math.Round(amount, accuracy);

            if (roundedValue != amount)
            {
                _logger.LogWarning(
                    "Amount was rounded to account base asset accuracy while starting [{OperationName}] operation: [{Amount}] -> [{RoundedValue}]",
                    operationName, amount, roundedValue);
            }

            return roundedValue;
        }
    }
}