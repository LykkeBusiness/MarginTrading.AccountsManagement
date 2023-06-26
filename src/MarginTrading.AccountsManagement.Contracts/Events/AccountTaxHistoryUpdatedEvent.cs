using System;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Models;
using MessagePack;

namespace MarginTrading.AccountsManagement.Contracts.Events
{
    /// <summary>
    /// Indicates that a recent tax transaction has been inserted to the account history.
    /// </summary>
    [MessagePackObject]
    public class AccountTaxHistoryUpdatedEvent
    {
        /// <summary>
        /// Operation Id - used as correlation id
        /// </summary>
        [Key(0)]
        public string OperationId { get; }

        /// <summary>
        /// Date and time of event
        /// </summary>
        [Key(1)]
        public DateTime ChangeTimestamp { get; }

        /// <summary>
        /// Account snapshot at the moment immediately after the event happened
        /// </summary>
        [NotNull]
        [Key(2)]
        public AccountContract Account { get; }

        public AccountTaxHistoryUpdatedEvent(string operationId, DateTime changeTimestamp, AccountContract account)
        {
            OperationId = operationId;
            ChangeTimestamp = changeTimestamp;
            Account = account;
        }

    } 
}