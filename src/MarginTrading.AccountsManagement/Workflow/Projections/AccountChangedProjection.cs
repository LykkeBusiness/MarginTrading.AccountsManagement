// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Logging;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Services;

namespace MarginTrading.AccountsManagement.Workflow.Projections
{
    public class AccountChangedProjection
    {
        private readonly IAccountManagementService _accountManagementService;
        private readonly ILogger _logger;

        public AccountChangedProjection(IAccountManagementService accountManagementService, ILogger<AccountChangedProjection> logger)
        {
            _accountManagementService = accountManagementService;
            _logger = logger;
        }

        [UsedImplicitly]
        public async Task Handle(AccountChangedEvent e)
        {
            var accountId = e?.Account.Id;

            if (string.IsNullOrEmpty(accountId))
            {
                _logger.LogWarning("Account id is empty, accountChangedEvent: {EventJson}", e.ToJson());
            }
            else
            {
                await _accountManagementService.ClearStatsCache(accountId);
            }
        }
    }
}