// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using static System.Math;
using System.Threading.Tasks;
using Common;

using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Workflow.NegativeProtection;
using MarginTrading.Backend.Contracts;

using Microsoft.Extensions.Internal;

namespace MarginTrading.AccountsManagement.Services.Implementation
{
    public class NegativeProtectionService : INegativeProtectionService
    {
        private readonly ISendBalanceCommandsService _sendBalanceCommandsService;
        private readonly ISystemClock _systemClock;
        private readonly bool _negativeProtectionAutoCompensation;
        private readonly IAccountsApi _accountsApi;
        
        public NegativeProtectionService(
            ISendBalanceCommandsService sendBalanceCommandsService,
            ISystemClock systemClock,
            AccountManagementSettings accountManagementSettings,
            IAccountsApi accountsApi)
        {
            _sendBalanceCommandsService = sendBalanceCommandsService;
            _systemClock = systemClock;
            _accountsApi = accountsApi;
            _negativeProtectionAutoCompensation = accountManagementSettings.NegativeProtectionAutoCompensation;
        }

        public async Task<decimal?> CheckAsync(string operationId,
            string accountId,
            decimal newBalance,
            decimal changeAmount)
        {
            if (newBalance >= 0 || changeAmount > 0)
                return null;

            var compensationAmount =
                // If the balance had already been negative before change happened
                newBalance < changeAmount
                    // we compensate only changeAmount
                    ? Abs(changeAmount)
                    // If the balance had been positive before change happened
                    // we compensate the difference
                    : Abs(newBalance);

            if (_negativeProtectionAutoCompensation)
            {
                var accountStats = await _accountsApi.GetAccountStats(accountId);
                if (!accountStats.IsInLiquidation)
                {
                    var auditLog = CreateCompensationAuditLog(DateTime.UtcNow);

                    await _sendBalanceCommandsService.ChargeManuallyAsync(
                        accountId,
                        compensationAmount,
                        $"{operationId}-negative-protection",
                        "Negative protection",
                        NegativeProtectionSaga.CompensationTransactionSource,
                        auditLog.ToJson(),
                        AccountBalanceChangeReasonType.CompensationPayments,
                        operationId,
                        null,
                        _systemClock.UtcNow.UtcDateTime
                    );   
                }
            }

            return compensationAmount;
        }

        private static object CreateCompensationAuditLog(DateTime timestamp)
        {
            const string systemUserName = "System";
            const string systemSessionId = "9999999999";
            
            return new
            {
                CreatedAt = timestamp,
                ApprovedAt = timestamp,
                CreatedBy = systemUserName,
                SessionId = systemSessionId,
                ApprovedBy = systemUserName
            };
        }
    }
}