// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Models;
using MessagePack;

namespace MarginTrading.AccountsManagement.Contracts.Events
{
    /// <summary>
    /// Something in the account has changed or the account has been created
    /// </summary>
    [MessagePackObject]
    public class AccountChangedEvent
    {
        /// <summary>
        /// OperationId is defined optional just to extend the message pack object - the parameter is still mandatory.  
        /// </summary>
        public AccountChangedEvent(DateTime changeTimestamp, 
            [NotNull] string source, 
            [NotNull] AccountContract account,
            AccountChangedEventTypeContract eventType, 
            AccountBalanceChangeContract balanceChange = null, 
            string operationId = null,
            string activitiesMetadata = null)
        {
            if (!Enum.IsDefined(typeof(AccountChangedEventTypeContract), eventType))
                throw new InvalidEnumArgumentException(
                    nameof(eventType),
                    (int) eventType,
                    typeof(AccountChangedEventTypeContract));

            if (changeTimestamp == default(DateTime))
                throw new ArgumentOutOfRangeException(nameof(changeTimestamp));

            Source = source;
            ChangeTimestamp = changeTimestamp;
            Account = account ?? throw new ArgumentNullException(nameof(account));
            EventType = eventType;
            ActivitiesMetadata = activitiesMetadata;
            BalanceChange = balanceChange;
            OperationId = operationId;
        }

        /// <summary>
        /// Date and time of event
        /// </summary>
        [Key(0)]
        public DateTime ChangeTimestamp { get; }

        /// <summary>
        /// Event sender process
        /// </summary>
        [Key(1)]
        public string Source { get; }
      
        /// <summary>
        /// Account snapshot at the moment immediately after the event happened
        /// </summary>
        [NotNull]
        [Key(2)]
        public AccountContract Account { get; }

        /// <summary>
        /// What happend to the account
        /// </summary>
        [Key(3)]
        public AccountChangedEventTypeContract EventType { get; }
      
        /// <summary>
        /// Account balance change details
        /// </summary>
        [CanBeNull]
        [Key(4)]
        public AccountBalanceChangeContract BalanceChange { get; }
        
        /// <summary>
        /// Operation Id
        /// </summary>
        [Key(5)]
        public string OperationId { get; }
        
        /// <summary>
        /// Serialised object with additional information for activities in any format
        /// </summary>
        [Key(6)]
        public string ActivitiesMetadata { get; }
    }
}