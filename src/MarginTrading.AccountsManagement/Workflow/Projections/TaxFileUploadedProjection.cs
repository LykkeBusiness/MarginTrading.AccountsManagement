// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using BookKeeper.Client.Workflow.Events;
using Common;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Repositories;

using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Workflow.Projections
{
    public class TaxFileUploadedProjection
    {
        private readonly IEodTaxFileMissingRepository _taxFileMissingRepository;
        private readonly ILogger _logger;

        public TaxFileUploadedProjection(IEodTaxFileMissingRepository taxFileMissingRepository,
            ILogger<TaxFileUploadedProjection> logger)
        {
            _taxFileMissingRepository = taxFileMissingRepository;
            _logger = logger;
        }

        [UsedImplicitly]
        public async Task Handle(TaxFileUploadedEvent e)
        {
            _logger.LogInformation("Handling tax file uploaded event: [{EventJson}]", e.ToJson());

            await _taxFileMissingRepository.RemoveAsync(e.TradingDay);
            
            _logger.LogInformation("Tax file missing entity has been deleted for trading day [{TradingDay}]", 
                e.TradingDay);
        }
    }
}