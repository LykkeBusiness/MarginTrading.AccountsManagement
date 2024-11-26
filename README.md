# MarginTrading.AccountsManagement API, AccountHistoryBroker #

API for account management. Broker to pass historical data from message queue to storage.
Below is the API description.

## How to use in prod env? ##

1. Pull "mt-accountsmanagement" docker image with a corresponding tag.
2. Configure environment variables according to "Environment variables" section.
3. Put secrets.json with endpoint data including the certificate:
```json
"Kestrel": {
  "EndPoints": {
    "HttpsInlineCertFile": {
      "Url": "https://*:5120",
      "Certificate": {
        "Path": "<path to .pfx file>",
        "Password": "<certificate password>"
      }
    }
}
```
4. Initialize all dependencies.
5. Run.

## How to run for debug? ##

1. Clone repo to some directory.
2. In MarginTrading.AccountsManagement root create a appsettings.dev.json with settings.
3. Add environment variable "SettingsUrl": "appsettings.dev.json".
4. VPN to a corresponding env must be connected and all dependencies must be initialized.
5. Run.

### Dependencies ###

TBD

### Configuration ###

Kestrel configuration may be passed through appsettings.json, secrets or environment.
All variables and value constraints are default. For instance, to set host URL the following env variable may be set:
```json
{
    "Kestrel__EndPoints__Http__Url": "http://*:5020"
}
```

### Environment variables ###

* *RESTART_ATTEMPTS_NUMBER* - number of restart attempts. If not set int.MaxValue is used.
* *RESTART_ATTEMPTS_INTERVAL_MS* - interval between restarts in milliseconds. If not set 10000 is used.
* *SettingsUrl* - defines URL of remote settings or path for local settings.

### Settings ###

AccountManagement settings schema is:
<!-- MARKDOWN-AUTO-DOCS:START (CODE:src=./service.json) -->
<!-- The below code snippet is automatically added from ./service.json -->
```json
{
  "APP_UID": "Integer",
  "ASPNETCORE_ENVIRONMENT": "String",
  "ENVIRONMENT": "String",
  "Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "String"
      }
    }
  },
  "MarginTradingAccountManagement": {
    "Behavior": {
      "AccountIdPrefix": "String",
      "BalanceResetIsEnabled": "Boolean",
      "DefaultBalance": "Integer",
      "DefaultWithdrawalIsEnabled": "Boolean"
    },
    "BrokerId": "String",
    "Cache": {
      "ExpirationPeriod": "DateTime",
      "RedisConfiguration": "String"
    },
    "ComplexityWarningExpiration": "String",
    "ComplexityWarningExpirationCheckPeriod": "DateTime",
    "ComplexityWarningsCount": "Integer",
    "Cqrs": {
      "ConnectionString": "String",
      "EnvironmentName": "String",
      "RetryDelay": "DateTime"
    },
    "Db": {
      "ConnectionString": "String",
      "LogsConnString": "String",
      "LongRunningSqlTimeoutSec": "Integer",
      "StorageMode": "String"
    },
    "EnableOperationsLogs": "Boolean",
    "LossPercentageCalculationEnabled": "Boolean",
    "LossPercentageCalculationPeriodInDays": "Integer",
    "LossPercentageExpirationCheckPeriodInDays": "Integer",
    "NegativeProtectionAutoCompensation": "Boolean",
    "OidcSettings": {
      "ApiAuthority": "String",
      "ClientId": "String",
      "ClientScope": "String",
      "ClientSecret": "String",
      "RenewTokenTimeoutSec": "Integer",
      "RequireHttps": "Boolean",
      "ValidateIssuerName": "Boolean"
    },
    "RabbitMq": {
      "AccountHistoryExchange": {
        "ConnectionString": "String",
        "ExchangeName": "String"
      },
      "BrokerSettings": {
        "ConnectionString": "String",
        "ExchangeName": "String",
        "QueueName": "String",
        "RoutingKey": "String"
      },
      "EodProcessFinished": {
        "ConnectionString": "String",
        "ExchangeName": "String",
        "QueueName": "String",
        "RoutingKey": "String"
      },
      "LossPercentageUpdated": {
        "ConnectionString": "String",
        "ExchangeName": "String",
        "IsDurable": "Boolean",
        "QueueName": "String",
        "RoutingKey": "String"
      },
      "NegativeProtection": {
        "ConnectionString": "String",
        "ExchangeName": "String"
      },
      "OrderHistory": {
        "ConnectionString": "String",
        "ExchangeName": "String",
        "QueueName": "String"
      }
    },
    "UseSerilog": "Boolean"
  },
  "MarginTradingAccountManagementServiceClient": {
    "ApiKey": "String",
    "ServiceUrl": "String"
  },
  "MarginTradingSettingsServiceClient": {
    "ApiKey": "String",
    "ServiceUrl": "String"
  },
  "MdmServiceClient": {
    "ApiKey": "String",
    "ServiceUrl": "String"
  },
  "MeteorServiceClient": {
    "ServiceUrl": "String"
  },
  "MtBackendServiceClient": {
    "ApiKey": "String",
    "ServiceUrl": "String"
  },
  "serilog": {
    "Enrich": [
      "String"
    ],
    "minimumLevel": {
      "default": "String"
    },
    "Properties": {
      "Application": "String"
    },
    "Using": [
      "String"
    ],
    "writeTo": [
      {
        "Args": {
          "configure": [
            {
              "Args": {
                "outputTemplate": "String"
              },
              "Name": "String"
            }
          ]
        },
        "Name": "String"
      }
    ]
  },
  "TradingHistoryClient": {
    "ApiKey": "String",
    "ServiceUrl": "String"
  },
  "TZ": "String"
}
```
<!-- MARKDOWN-AUTO-DOCS:END -->

AccountHistoryBroker settings schema is:
<!-- MARKDOWN-AUTO-DOCS:START (CODE:src=./broker.json) -->
<!-- The below code snippet is automatically added from ./broker.json -->
```json
{
  "APP_UID": "Integer",
  "ASPNETCORE_ENVIRONMENT": "String",
  "ENVIRONMENT": "String",
  "IsLive": "Boolean",
  "Kestrel": {
    "EndPoints": {
      "Http": {
        "Url": "String"
      }
    }
  },
  "MtBrokerSettings": {
    "AccountManagement": {
      "ServiceUrl": "String"
    },
    "ConsumerCount": "Integer",
    "Db": {
      "ConnString": "String",
      "StorageMode": "String"
    },
    "MtRabbitMqConnString": "String",
    "RabbitMq": {
      "AccountTaxHistoryUpdated": {
        "ConnectionString": "String",
        "ExchangeName": "String",
        "IsDurable": "Boolean",
        "RoutingKey": "String"
      }
    },
    "RabbitMqQueues": {
      "AccountHistory": {
        "ExchangeName": "String"
      }
    }
  },
  "MtBrokersLogs": {
    "LogsConnString": "String",
    "StorageMode": "String",
    "UseSerilog": "Boolean"
  },
  "serilog": {
    "Enrich": [
      "String"
    ],
    "minimumLevel": {
      "default": "String"
    },
    "Properties": {
      "Application": "String"
    },
    "Using": [
      "String"
    ],
    "writeTo": [
      {
        "Args": {
          "configure": [
            {
              "Args": {
                "outputTemplate": "String"
              },
              "Name": "String"
            }
          ]
        },
        "Name": "String"
      }
    ]
  },
  "TZ": "String"
}
```
<!-- MARKDOWN-AUTO-DOCS:END -->
