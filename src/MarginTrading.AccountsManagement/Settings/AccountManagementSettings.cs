// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using JetBrains.Annotations;
using Lykke.Common.Chaos;
using Lykke.SettingsReader.Attributes;

namespace MarginTrading.AccountsManagement.Settings
{
    [UsedImplicitly]
    public class AccountManagementSettings
    {
        /// <summary>
        /// DB connection strings
        /// </summary>
        public DbSettings Db { get; set; }
        
        /// <summary>
        /// RabbitMq exchanges connections
        /// </summary>
        public RabbitMqSettings RabbitMq { get; set; }
        
        /// <summary>
        /// Behavior settings for accounts
        /// </summary>
        [Optional, CanBeNull]
        public BehaviorSettings Behavior { get; set; }
        
        public CqrsSettings Cqrs { get; set; }
        
        [Optional]
        public bool EnableOperationsLogs { get; set; }

        /// <summary>
        /// Shows if negative account capital will be automatically compensated.
        /// Enabled by default.
        /// </summary>
        [Optional] 
        public bool NegativeProtectionAutoCompensation { get; set; } = true;

        [Optional, CanBeNull]
        public ChaosSettings ChaosKitty { get; set; }
        
        [Optional]
        public bool UseSerilog { get; set; }
        
        public CacheSettings Cache { get; set; }

        public string BrokerId { get; set; }

        [Optional] 
        public int ComplexityWarningsCount { get; set; } = 2;

        [Optional]
        public TimeSpan ComplexityWarningExpiration { get; set; } = TimeSpan.FromDays(3 * 365);

        [Optional]
        public TimeSpan ComplexityWarningExpirationCheckPeriod { get; set; } = TimeSpan.FromMinutes(5);

        [Optional]
        public TimeSpan LossPercentageExpirationCheckPeriod { get; set; } = TimeSpan.FromDays(30 * 3);
        
        [Optional]
        public TimeSpan LossPercentageCalculationPeriod { get; set; } = TimeSpan.FromDays(365);
        
        [Optional]
        public bool LossPercentageCalculationEnabled { get; set; } = false;
    }
}
