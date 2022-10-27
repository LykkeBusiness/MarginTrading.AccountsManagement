// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Logging;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Repositories;
using MarginTrading.Backend.Contracts.TradingSchedule;

namespace MarginTrading.AccountsManagement.Workflow.Projections
{
    public class MarketStateChangedProjection
    {
        private readonly IEodTaxFileMissingRepository _taxFileMissingRepository;
        private readonly ILogger _logger;
        
        private const string PlatformScheduleMarketId = nameof(PlatformScheduleMarketId);

        public MarketStateChangedProjection(ILogger<MarketStateChangedProjection> logger, IEodTaxFileMissingRepository taxFileMissingRepository)
        {
            _logger = logger;
            _taxFileMissingRepository = taxFileMissingRepository;
        }
        
        [UsedImplicitly]
        public async Task Handle(MarketStateChangedEvent e)
        {
            _logger.LogInformation("Handling new MarketStateChanged event: [{EventJson}]",e.ToJson());

            if (e.Id == PlatformScheduleMarketId && e.IsEnabled)
            {
                var tradingDay = e.EventTimestamp.Date;
                    
                await _taxFileMissingRepository.AddAsync(tradingDay);
                
                _logger.LogInformation("Added new tax file missing day: [{TradingDay}]", tradingDay);
            }
        }
    }
}