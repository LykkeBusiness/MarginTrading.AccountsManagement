// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.SettingsReader.Attributes;

namespace MarginTrading.AccountsManagement.Settings
{
    public class ExtendedLoggingSettings
    {
        /// <summary>
        /// Enables detailed logging for the Taxes Saga
        /// </summary>
        [Optional]
        public bool TaxesLoggingEnabled { get; set; }
    }
}