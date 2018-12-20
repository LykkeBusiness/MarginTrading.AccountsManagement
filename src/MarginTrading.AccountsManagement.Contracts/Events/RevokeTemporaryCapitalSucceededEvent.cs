using System;
using JetBrains.Annotations;
using MessagePack;

namespace MarginTrading.AccountsManagement.Contracts.Events
{
    [MessagePackObject]
    public class RevokeTemporaryCapitalSucceededEvent : BaseEvent
    {
        public RevokeTemporaryCapitalSucceededEvent([NotNull] string operationId, DateTime eventTimestamp,
            string eventSourceId, string accountId, string revokeEventSourceId, string auditLog)
            : base(operationId, eventTimestamp)
        {
            EventSourceId = eventSourceId;
            AccountId = accountId;
            RevokeEventSourceId = revokeEventSourceId;
            AuditLog = auditLog;
        }

        [Key(2)] 
        public string EventSourceId { get; }

        [Key(3)] 
        public string AccountId { get; }

        [Key(4)] 
        public string RevokeEventSourceId { get; }

        [Key(5)] 
        public string AuditLog { get; }
    }
}