using System;

using JetBrains.Annotations;

using Lykke.SettingsReader.Attributes;

namespace MarginTrading.AccountsManagement.IntegrationTests.Settings
{
    public class CqrsSettings
     {
         public string ConnectionString { get; set; }
 
         public TimeSpan RetryDelay { get; set; }
 
         [Optional, CanBeNull]
         public string EnvironmentName { get; set; }
 
         [Optional]
         public CqrsContextNamesSettings ContextNames { get; set; } = new CqrsContextNamesSettings();
     }
 }