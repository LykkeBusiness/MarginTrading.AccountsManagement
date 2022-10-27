// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Logging;
using JsonDiffPatchDotNet;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Repositories;

namespace MarginTrading.AccountsManagement.Services.Implementation
{
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository _auditRepository;
        private readonly ILogger _logger;

        public AuditService(IAuditRepository auditRepository, ILogger<AuditService> logger)
        {
            _auditRepository = auditRepository;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public Task<PaginatedResponse<AuditModel>> GetAll(AuditLogsFilterDto filter, int? skip, int? take)
        {
            (skip, take) = ValidateSkipAndTake(skip, take);

            return _auditRepository.GetAll(filter, skip, take);
        }

        public Task TryAuditTradingConditionUpdate(string correlationId,
            string userName,
            string clientId,
            string newTradingConditionId,
            string oldTradingConditionId)
        {
            return TryAudit(correlationId,
                userName,
                clientId,
                AuditDataType.ClientProfileAssignment,
                new {clientId, tradingConditionId = newTradingConditionId}.ToJson(),
                new {clientId, tradingConditionId = oldTradingConditionId}.ToJson());
        }

        private async Task<bool> TryAudit(
            string correlationId,
            string userName,
            string referenceId,
            AuditDataType type,
            string newStateJson = null,
            string oldStateJson = null)
        {
            if (string.IsNullOrEmpty(newStateJson) && string.IsNullOrEmpty(oldStateJson))
            {
                _logger.LogWarning("Unable to generate audit event based on both newStateJson and oldStateJson state as null");
                return false;
            }

            var auditModel = BuildAuditModel(correlationId, userName, DateTime.UtcNow, referenceId, type, newStateJson,
                oldStateJson);

            if (auditModel == null)
                return false;

            await _auditRepository.InsertAsync(auditModel);

            return true;
        }

        private static AuditModel BuildAuditModel(
            string correlationId,
            string userName,
            DateTime timestamp,
            string referenceId,
            AuditDataType dataType,
            string newStateJson,
            string oldStateJson)
        {
            var eventType = AuditEventType.Edition;

            if (string.IsNullOrEmpty(oldStateJson))
            {
                eventType = AuditEventType.Creation;
                oldStateJson = "{}";
            }

            if (string.IsNullOrEmpty(newStateJson))
            {
                eventType = AuditEventType.Deletion;
                newStateJson = "{}";
            }

            var jdp = new JsonDiffPatch();
            var diffResult = jdp.Diff(oldStateJson, newStateJson);

            if (string.IsNullOrEmpty(diffResult))
                return null;

            return new AuditModel
            {
                Timestamp = timestamp,
                CorrelationId = correlationId,
                UserName = userName,
                Type = eventType,
                DataType = dataType,
                DataReference = referenceId,
                DataDiff = diffResult
            };
        }

        private static (int? skip, int? take) ValidateSkipAndTake(int? skip, int? take)
        {
            if (skip.HasValue && skip.Value < 0)
                skip = 0;

            if (skip.HasValue && !take.HasValue)
                take = 20;

            if (!skip.HasValue && take.HasValue)
                skip = 0;

            if (take.HasValue && take.Value <= 0)
                take = 20;

            return (skip, take);
        }
    }
}