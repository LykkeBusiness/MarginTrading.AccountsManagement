## 2.24.2 - Nova 2. Delivery 47. Hotfix 3 (January 21, 2025)
### What's changed
* LT-6018: Account History Broker doesn't start


## 2.24.1 - Nova 2. Delivery 47. Hotfix 2 (January 15, 2025)
### What's changed
* LT-5991: Bump LykkeBiz.RabbitMqBroker to 8.11.1


## 2.24.0 - Nova 2. Delivery 47 (November 15, 2024)
### What's changed
* LT-5813: Update messagepack to 2.x version.
* LT-5750: Add assembly load logger.
* LT-5716: Migrate to quorum queues.

### Deployment
In this release, all previously specified queues have been converted to quorum queues to enhance system reliability. The affected queues are:
- `lykke.mt.orderhistory.MarginTrading.AccountsManagement`
- `dev.MdmService.queue.BrokerSettingsChangedEvent`
- `dev.BookKeeper.queue.EodProcessFinishedEvent`
- `dev.AccountsManagement.queue.LossPercentageUpdated`

#### Automatic Conversion to Quorum Queues
The conversion to quorum queues will occur automatically upon service startup **if**:
* There are **no messages** in the existing queues.
* There are **no active** subscribers to the queues.

**Warning**: If messages or subscribers are present, the automatic conversion will fail. In such cases, please perform the following steps:
1. Run the previous version of the component associated with the queue.
1. Make sure all the messages are processed and the queue is empty.
1. Shut down the component associated with the queue.
1. Manually delete the existing classic queue from the RabbitMQ server.
1. Restart the component to allow it to create the quorum queue automatically.

#### Poison Queues
All the above is also applicable to the poison queues associated with the affected queues. Please ensure that the poison queues are also converted to quorum queues.

#### Disabling Mirroring Policies
Since quorum queues inherently provide data replication and reliability, server-side mirroring policies are no longer necessary for these queues. Please disable any existing mirroring policies applied to them to prevent redundant configurations and potential conflicts.

#### Environment and Instance Identifiers
Please note that the queue names may include environment-specific identifiers (e.g., dev, test, prod). Ensure you replace these placeholders with the actual environment names relevant to your deployment. The same applies to instance names embedded within the queue names (e.g., DefaultEnv, etc.).


## 2.23.0 - Nova 2. Delivery 46 (September 27, 2024)
### What's changed
* LT-5589: Migrate to net 8.


## 2.22.0 - Nova 2. Delivery 44 (August 16, 2024)
### What's changed
* LT-5525: Update rabbitmq broker library with new rabbitmq.client and templates.

### Deployment
**Configuration updates**:
- Remove configuration section `MarginTradingAccountManagement.RabbitMQ.AccountChangedExchange`. It is not used.
- Add `QueueName` key into configuration section `MarginTradingAccountManagement.RabbitMQ.OrderHistory` with value `lykke.mt.orderhistory.MarginTrading.AccountsManagement`.
- Add `QueueName` key into configuration section `MarginTradingAccountManagement.RabbitMQ.BrokerSettings` with value `dev.MdmService.queue.BrokerSettingsChangedEvent`.
- Add `QueueName` key into configuration section `MarginTradingAccountManagement.RabbitMQ.EodProcessFinished` with value `dev.BookKeeper.queue.EodProcessFinishedEvent`.

Please ensure that the mirroring policy is configured on the RabbitMQ server side for the following queues:
- `lykke.mt.orderhistory.MarginTrading.AccountsManagement`
- `dev.MdmService.queue.BrokerSettingsChangedEvent`
- `dev.BookKeeper.queue.EodProcessFinishedEvent`
- `dev.AccountsManagement.queue.LossPercentageUpdated`

These queues require the mirroring policy to be enabled as part of our ongoing initiative to enhance system reliability. They are now classified as "no loss" queues, which necessitates proper configuration. The mirroring feature must be enabled on the RabbitMQ server side.

In some cases, you may encounter an error indicating that the server-side configuration of a queue differs from the client’s expected configuration. If this occurs, please delete the queue, allowing it to be automatically recreated by the client.

**Warning**: The "no loss" configuration is only valid if the mirroring policy is enabled on the server side.

Please be aware that the provided queue names may include environment-specific identifiers (e.g., dev, test, prod). Be sure to replace these with the actual environment name in use. The same applies to instance names embedded within the queue names (e.g., DefaultEnv, etc.).


## 2.21.0 - Nova 2. Delivery 43 (June 03, 2024)
### What's changed
* LT-5462: An error in the log "string or binary data would be truncated"


## 2.20.0 - Nova 2. Delivery 41 (March 29, 2024)
### What's changed
* LT-5324: Additional logging for taxes saga.
* LT-5278: Adjust the `iaccountbalancehistoryapi` api.

### Deployment
* For both **AccountsManagement** and **AccountHistory Broker**:

Add a new configuration section `ExtendedLoggingSettings`, then set `TaxesLoggingEnabled` to `true`

Example for **AccountsManagement**

```
{
 "MarginTradingAccountManagement": 
  {
    "ExtendedLoggingSettings": 
    {
      "TaxesLoggingEnabled": true
    },

    // omitted
```
Example for **AccountHistory Broker**
```
{
  "MtBrokerSettings": 
  {
    "ExtendedLoggingSettings": 
    {
      "TaxesLoggingEnabled": true
    },

    // omitted
```


## 2.19.0 - Nova 2. Delivery 40 (February 28, 2024)
### What's changed
* LT-5293: [AccountHistoryBroker] Fix "update version number" and "deprecated packages validation" build steps
* LT-5290: [AccountManagment] Fix "update version number" and "deprecated packages validation" build steps


## 2.18.0 - Nova 2. Delivery 39 (January 26, 2024)
### What's changed
* LT-5175: Exclude unconfirmedmargin from disposable.
* LT-5164: Accounts management service: add history of releases into changelog.md for github.


## 2.17.2 - Nova 2. Delivery 38 (December 13, 2023)
### What's changed
* LT-4956: Modification of comment section in cash movements.

### Deployment
* Execute the following SQL script to update the database:
```sql
update [dbo].[AccountHistory]
set Comment = REPLACE(REPLACE(Comment, 'Funds deposit ', ''), 'Funds withdrawal ', '')
where ReasonType in ('Deposit', 'Withdraw') and Comment like 'Funds %'
```


## 2.16.2 - Nova 2. Delivery 37 (2023-10-17)
### What's changed
* LT-4992: Add and handle new flag shouldshow871mwarning.

### Deployment
* Add new settings to the configuration root:
```json
{
"MeteorServiceClient": 
  {
    "ServiceUrl": "http://<meteor-host-uri>",
    "ApiKey": "<your-api-key>"
  }
}
```
* Add new configuration inside MarginTradingAccountManagement section:
```json
{
"OidcSettings": 
    {
      "ApiAuthority": "http://<bouncer-host-uri>",
      "ClientId": "accounts_management_api",
      "ClientSecret": "<your-client-secret>",
      "ClientScope": "meteor_api meteor_api:server",
      "ValidateIssuerName": false,
      "RequireHttps": false,
      "RenewTokenTimeoutSec": 1800
    }
}
```
* To configure Bouncer the following command has to be executed:
```
./auth clients add server accounts_management_api -n "Accounts Management API" -s <your-secret-key> -a openid -a profile -a meteor_api -a meteor_api:server -t Jwt
```

**Full change log**: https://github.com/lykkebusiness/margintrading.accountsmanagement/compare/v2.16.1...v2.16.2

## v2.16.1 - Nova 2. Delivery 36. Hotfix 3
## What's changed
* LT-5009: Update `GetDisposableCapital` endpoint

**Full Changelog**: https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/compare/v2.16.0...v2.16.1

## v2.16.0 - Nova 2. Delivery 36
## What's changed
* LT-4897: Update nugets.
* LT-4809: New referenceaccount column.
* LT-4804: Publish event when tax history is added.

## Deployment
Update `appsettings.json` to include the following section:
```json
{
  "RabbitMq": {
    "AccountTaxHistoryUpdated": {
      "ConnectionString": "[rabbitmq-broker-connection-string]",
      "ExchangeName": "[env].AccountsManagement.events.exchange",
      "IsDurable": true
    }
  }
}
```

**Full change log**: https://github.com/lykkebusiness/margintrading.accountsmanagement/compare/v2.15.0...v2.16.0

## v2.15.0 - Nova 2. Delivery 35
## What's changed
* LT-4849: Bump lykke.snow.common -> 2.7.3.


**Full change log**: https://github.com/lykkebusiness/margintrading.accountsmanagement/compare/v2.14.1...v2.15.0

## v2.14.1 - Nova 2. Delivery 34
## What's Changed

- LT-4733: Upgrade Lykke.MarginTrading.AssetService.Contracts

**Full Changelog**: https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/compare/v2.13.1...v2.14.1

## v2.13.1 - Nova 2. Delivery 33
## What's changed
* LT-4549: Accounts management balance update logs are inaccurate.
* LT-4542: Update deposit/withdrawal event payloads.
* LT-4390: Migrate brokerbase.
* LT-4273: Account management.


**Full change log**: https://github.com/lykkebusiness/margintrading.accountsmanagement/compare/v2.12.4...v2.13.1

## v2.12.4 - Nova 2. Delivery 32
## What's changed
* LT-4540: Put back host restart attempts.
* LT-4400: Do not let the host keep running if startupmanager failed to start.
* LT-4224: Validateskipandtake implementation replace.


**Full change log**: https://github.com/lykkebusiness/margintrading.accountsmanagement/compare/v2.11.10...v2.12.4

## v2.11.10 - Nova 2. Delivery 31. Hotfix 2
## What's changed

* LT-4395: Update AccountUpdatedEvent Payload

## Deployment

Run the following SQL script before running the new version:
```sql
ALTER TABLE MarginTradingClients
ADD ModificationTimestamp DATETIME NOT NULL default CURRENT_TIMESTAMP;

UPDATE MarginTradingClients
SET ModificationTimestamp = (SELECT COALESCE(MAX(MarginTradingAccounts.ModificationTimestamp), GETUTCDATE()) 
FROM MarginTradingAccounts WHERE MarginTradingAccounts.ClientId = MarginTradingClients.Id)
```

**Full Changelog**: https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/compare/v2.11.9...v2.11.10

## v2.11.9 - Nova 2. Delivery 31. Hotfix 1
## What's Changed
* LT-4392: fix missing logs

**Full Changelog**: https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/compare/v2.11.7...v2.11.9

## v2.11.8 - Nova 2. Delivery 29. Hotfix 5
## What's changed

- LT-4391: fix missing logs

**Full Changelog**: https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/compare/v2.11.6...v2.11.8

## v2.11.7 - Nova 2. Delivery 31
## What's changed
* LT-4373: Update messaging libraries to fix verify endpoints.
* LT-4367: Add withdrawal logs.
* LT-4257: Subscribe to eod to calculate loss percentage if needed.
* LT-4242: Temporary capital breaks sql select when account is not found.

### Deployment

* Added new configuration keys, all optional:
  - `LossPercentageExpirationCheckPeriodInDays`, default = 90 
  - `LossPercentageCalculationPeriodInDays`, default = 365;
  - `LossPercentageCalculationEnabled`, default = false

* Added new configuration section MarginTradingAccountManagement-> RabbitMq-> EodProcessFinished:
```json
{
  "EodProcessFinished":  {
        "ConnectionString": "Broker-RabbitMq-ConnectionString",
        "ExchangeName": "dev.BookKeeper.events.exchange",
        "RoutingKey": "EodProcessFinishedEvent"
  }
}
```
* Added new configuration section MarginTradingAccountManagement-> RabbitMq-> LossPercentageUpdated:
```json
{
  "LossPercentageUpdated": {
        "ConnectionString": "Global-RabbitMq-ConnectionString",
        "ExchangeName": "dev.AccountsManagement.events.exchange",
        "QueueName": "dev.AccountsManagement.queue.LossPercentageUpdated",
        "RoutingKey": "LossPercentageUpdated",
        "IsDurable": true
  }
}
```

**Full change log**: https://github.com/lykkebusiness/margintrading.accountsmanagement/compare/v2.11.6...v2.11.7

## v2.11.6 - Nova 2. Delivery 29. Hotfix 4
### What's changed
* LT-4351: Add diagnostic logging when calculating disposable capital

**Full Changelog**: https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/compare/v2.11.5...v2.11.6

## v2.11.4 - Nova 2. Delivery 28. Hotfix 5
* LT-4333: create missing CQRS RabbitMq queues and exchanges

## v2.11.2 - Nova 2. Delivery 28. Hotfix 3
## What's Changed

* LT-4318:  Upgrade LykkeBiz.Logs.Serilog to 3.3.1

**Full Changelog**: https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/compare/v2.11.0...v2.11.2

## v2.11.0 - Nova 2. Delivery 28
## What's Changed
* LT-3721: NET 6 migration

### Deployment
* NET 6 runtime is required
* Dockerfile is updated to use native Microsoft images (see [DockerHub](https://hub.docker.com/_/microsoft-dotnet-runtime/))


**Full Changelog**: https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/compare/v2.10.3...v2.11.0

## v2.10.3 - Nova 2. Delivery 27
## What's Changed
* LT-4142: update core contracts package


**Full Changelog**: https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/compare/v2.9.3...v2.10.3

## v2.9.4 - Nova 2. Delivery 26
## What's Changed
* feat(LT-4015): add missing taxfile days controller by @gponomarev-lykke in https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/pull/162


**Full Changelog**: https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/compare/v2.9.2...v2.9.4

## Deployment
* New api added: GET /api/missing-taxfile-days

## v2.9.2 - v2.9.2
## What's Changed
* LT-3951: Disposable capital by @gponomarev-lykke in https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/pull/161
* LT-3922: Rename session id by @gponomarev-lykke in https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/pull/160
* LT-3894: Upgrade Lykke.HttpClientGenerator nuget by @lykke-vashetsin in https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/pull/159


**Full Changelog**: https://github.com/LykkeBusiness/MarginTrading.AccountsManagement/compare/v2.9.1...v2.9.2

## Deployment
New RabbitMq configuration section to be added:
```json
{
 "BrokerSettings": { 
  "ConnectionString": "global_rabbit_mq_connection_string", 
  "ExchangeName": "mdm_service_events_exchange", 
  "RoutingKey": "BrokerSettingsChangedEvent" 
 }
}
```

## v2.9.1 - Nova 2. Delivery 23
* LT-3918: Extend /api/balance-history/by-pages/{accountId}
* LT-3855: Add GetDisposableCapital api
* LT-3846: Add new api that returns clients with accounts

## v2.9.0 - Nova 2. Delivery 22
* LT-3779:  Add api /api/accounts/suggested
* LT-3792: Add api to fetch balance change histories by date

## v2.8.3 - Nova 2. Delivery 21. Hotfix 2
* LT-3814: Assume dates are in Utc when kind is unspecified

## v2.8.2 - Nova 2. Delivery 21
* LT-3761: Exclude deleted accounts from `GetCachedAccountStatistics`
* LT-3729: NOVA security threats

## v2.8.1 - Nova 2. Delivery 20
* LT-3693: CorrelationId code cleanup

## v2.8.0 - Correlation IDs
### Tasks

* LT-3453: Correlation IDs

## v2.7.2 - Nova 2. Delivery 18.
### Tasks

* LT-3549: Extend API to get clients with trading conditions
* LT-3568: Extend "create account" request with broker user id field

### Deployment

* Execute SQL script to fill in new UserId field (ONLY AFTER SERVICE START):
````sql
UPDATE c
    SET c.UserId = u.Username
    FROM
        [dbo].[MarginTradingClients] c
    INNER JOIN
        [bouncer].[AspNetUsers] u ON c.Id = u.Id
````
* Contracts nuget package version 6.6.0 published. 


## v2.7.0 - Nova 2. Delivery 17.
* LT-3536: Opened positions when assigning client profile

## v2.6.3 - Nova 2. Delivery 16.
* LT-3268: Created_at is wrong for negative compensations

## v2.6.1 - Nova 2. Delivery 13.
### Tasks
* LT-3286: Improve EOD performance for charging PnL and overnight Swap accounts

## v2.6.0 - Delivery 11.
### Tasks

* LT-3209: MarginTrading.AccountsManagement: change naming convention for stored procedure
* LT-3172: Add AuditTrail for client profiles assignment
* LT-3214: Add order ID to account changed event
* LT-3219: Run compensation on account balance update
* LT-3229: Wrong timestamp for "account" activities

### Deployment

* After the deployement delete [dbo].[SP_UpdateDealCommissionParamsOnAccountHistory] from db
* dev.AccountsManagement.NegativeProtectionSaga.queue.TradingEngine.events.self queue is no longer used (need to delete)

## v2.5.0 - Nova 2. Delivery 10.
### Tasks

* LT-3151: Add endpoint to fetch the list of client accounts connected to client profile
* LT-3155: Bulk Edit profiles

## v2.4.0 - Nova 2. Delivery 9.
### Tasks

* LT-3138: AccountManagment: there is an 404 error when we make instrument unavailable via changing profile in case when investor has opened order or position
* LT-3133: Fix client trading conditions client
* LT-3011: Store confirmation flag on the account
* LT-3130: Add TZ = Europe/Berlin for all our services

### Deployment

* Run script
```sql
alter table MarginTradingAccounts add AdditionalInfo nvarchar (max) null
go
update MarginTradingAccounts set AdditionalInfo = '{}' where AdditionalInfo is null
alter table MarginTradingAccounts alter column AdditionalInfo nvarchar (max) not null
```

* Add to settings MarginTradingAccountManagement.RabbitMq' section:
```json
"OrderHistory": {
"ConnectionString": "${MtRabbitMqConnectionString}",
"ExchangeName": "lykke.mt.orderhistory"
}
```

* (optional) Configure params for product complexity feature:
Fill values in MarginTradingAccountManagement section
"ComplexityWarningsCount": 2 - (default 2)
"ComplexityWarningExpiration": "1095.00:00:00" (default 3 years 3 *365 =1095)
"ComplexityWarningExpirationCheckPeriod": "00:05:00" (default 5 minutes)

## v2.2.4 - Delivery 6. Hotfix 1.
### Tasks

* LT-3143: Disposable capital wrong for negative unrealized pnL

## v2.3.3 - Delivery 8. Hotfix 1.
### Tasks

* LT-3143: Disposable capital wrong for negative unrealized pnL

## v2.3.2 - Nova 2. Delivery 8.
### Tasks

* LT-3035: Profiles Assignment. API and Storage
* LT-3134: AccountsManagement: Duplicate results when searching for an account

### Deployment

Run script BEFORE deployment of the new version
```sql
--Creates table MarginTradingClients
if object_id('dbo.MarginTradingClients', 'U') is null 
begin   
    create table dbo.MarginTradingClients (
        Id nvarchar(64) not null primary key,
        TradingConditionId nvarchar(64) not null		 
    );
end;

--Migrates ClientId/TradingConditionId MarginTradingAccounts -> MarginTradingClients
;with cte as
(
   select 
		ClientId,
		a.TradingConditionId,
        row_number() over (partition by a.ClientId order by a.ClientId DESC) AS rn
	from dbo.MarginTradingAccounts a 
	left join dbo.MarginTradingClients c on a.ClientId = c.Id
	where c.Id is null
)
insert into MarginTradingClients (Id, TradingConditionId)
select 
	ClientId,
	TradingConditionId
from cte
where rn = 1

-- Adds Constraint for MarginTradingAccounts -> MarginTradingClients
if object_id('dbo.[FK_MarginTradingAccounts_MarginTradingClients]', 'F') is null 
begin   
	alter table [dbo].[MarginTradingAccounts]  with check add constraint [FK_MarginTradingAccounts_MarginTradingClients] foreign key ([ClientId])
	references [dbo].[MarginTradingClients] ([Id])
end;

-- Drop table from MarginTradingAccounts as now we have one in MarginTradingClients
alter table  dbo.[MarginTradingAccounts] drop column if exists TradingConditionId

```

## v2.2.1 - Nova 2. Delivery 6.
### Tasks

* LT-2911: Capital Figures: realizedPnlDay is still 0 in /accounts after closing a position
* LT-2913: Compensation amount is not deducted from disposableCapital or deducted with delay
* LT-2934: Modify disposable capital calculation

### Deployment

* Modify settings of Account History Broker. 

Add to MtBrokerSettings new section:

```json
"AccountManagement": {
"ServiceUrl": "mt-account-management-url",
"ApiKey": "secret-key",
}
```

* Modify settings of Account Management Service

Add to MarginTradingAccountManagement.Cache:

```json
"RedisConfiguration": "redis-connection-string"
```

## v2.1.0 - Nova 2. Delivery 5.
### Tasks

* LT-2916: Add information about the disposable capital calculations to the API
* LT-2853: Add accountName to the account object
* LT-2855: Change withdrawal validations
* LT-2873: UseAccountName for support users search when switching accounts on behalf
* LT-2900: Update API to return typed error response for accounts management

### Deployment

* SQL script should be executed in the DB before deployment:
```sql
ALTER TABLE dbo.MarginTradingAccounts
ADD AccountName [nvarchar] (255);
```

* Add `MdmServiceClient` section to the settings root
```json
"MdmServiceClient": {
"ServiceUrl": "http://mdm.mt.svc.cluster.local",
"ApiKey": "mdm_secretkey"
}
```

* Add "BrokerId" field to MarginTradingAccountManagement section
```json
"BrokerId": "Consors"
```

## v2.0.1 - New error type
### Tasks

* LT-2466: MarginTrading.AccountsManagement: add new error type when user tries to withdraw with locked operation

## v2.0.0 - Updated Asset Service contracts
### Tasks

* LT-2398: Rename Settings Service to Asset Service. Update the contracts package.

## v1.16.5 - Withdrawal validations
### Tasks

* LT-2382: Take into account days when taxes has not been uploaded yet when making a withdrawal

### Deployment

* Added new key CqrsSettings.ContextNames.BookKeeper, default value is "BookKeeper".
* Requires Lykke.Snow.Common package 1.1.1
* Requires Lykke.MarginTrading.BookKeeper.Contracts 1.1.2
* Depends on BookKeeper nova-2.15.2


### RabbitMQ
There are 2 new RabbitMq bindings
* Exchange dev.BookKeeper.events.exchange, queue dev.AccountsManagement.queue.BookKeeper.events.events.projections, routing key TaxFileUploadedEvent
* Exchange dev.TradingEngine.events.exchange, queue dev.AccountsManagement.queue.TradingEngine.events.events.projections, routing key MarketStateChangedEvent

## v1.16.4 - Validation improvements
### Tasks

* LT-2312: Change withdrawal validations
* LT-2422: Add business exception with error code (relates to LT-2421: User's account can be locked with active positions)

### Deployment

* New section should be added to the route of the settings.

Example:
````json
"TradingHistoryClient": {
"ServiceUrl": "http://mt-tradinghistory.mt.svc.cluster.local",
"ApiKey": "margintrading"
}
````

* New connection between services. 
From now on Account Service will call Trading History service for withdrawal validations.

* Depends on Trading History service v1.14.8

## v1.16.2 - Bugfix
### Tasks

* LT-2280:  Update Lykke.Logs.MsSql

## v1.16.1 - Update libraries to avoid sockets leakage
### Tasks

LT-2245: Update libraries to avoid sockets leakage

## v1.16.0 - Migration to .NET 3.1
### Tasks

LT-2170: Migrate to 3.1 Core and update DL libraries

## v1.15.17 - Bugfix
### Tasks

LT-2210: Information about an account (stats) when it has a negative balance is not displayed

## v1.15.16 - Improvement
### Tasks

LT-2190: include isAscendingOrder to ListByPages method

## v1.15.15 - Improvements
### Tasks

LT-2176: Extend AccountStat contract, add DisposableCapital field
LT-2155: Fix threads leak with RabbitMq subscribers and publishers

## v1.15.14 - Account stats and history optimisations
### Tasks

LT-2139: Performance issue when working with AccountHistory table

### Deployment

Added new setting:

````json 
"Cache": {
"ExpirationPeriod": "12:00:00"
}
````
In order to optimize accounts statistics request, we introduced the cache, that keeps the values for the current day by key = account id + date. The cache is invalidated on every account balance change event + after ExpirationPeriod. Due to that, ExpirationPeriod should not be very small. The recommended value is 12h, so that cache size will not grow and data for the previous day will be removed the next day, but at the same time, data should not expire too frequently, because it will increase the load.

## v1.15.13 - Critical fixes
### Tasks
LT-2135: Missing realised PnL for some clients

## v1.15.12 - Improvements
### Tasks

LT-2014: Improve Alpine docker files
LT-2113: MarginTrading.AccountsManagement: update brokerbase nuget
LT-2118: Consider compensation payments in withdrawal validations

## v1.15.11 - Alpine docker image
### Tasks

LT-1929: [MT-AM] Update cqrs libraries
LT-1988: Migrate to Alpine docker images in MT Core services

## v1.15.9 - Improvements, bugfixes
### Tasks

LT-1905: Provide configuration for Microsoft logs in Mt-core services (related to LT-1852)
LT-1898: BE. Withdraw not possible without comment (related to LT-1886)
LT-1895: MarginTrading.AccountsManagement: fix "Timeout expired" error

### Deployment

Add new settings to "MarginTradingAccountManagement" -> "Db":
````json
"LongRunningSqlTimeoutSec": 120
````

### Dependencies

Withdraw without comment will work only when **Trading Core v1.16.28** will be released

## v1.15.7 - API for messages resend
### Tasks

LT-1840: Message lost on replay after moved to poison queue

## v1.15.6 - Bugfix
### Tasks

LT-1816: account history broker fails on swap calculation when setting deal params + test code to produce cqrs message

## v1.15.5 - Bugfix + update .net core
### Tasks

LT-1759: update .NET to version 2.2
LT-1748: Deadlocks with deals

### After deployment of AccountsManagement + TradingHistory 
- make sure there were no exceptions in PositionHistoryBroker & TradingHistory
- apply alters:
````sql
DROP PROCEDURE SP_InsertAccountHistory;
DROP PROCEDURE SP_InsertDeal;

INSERT INTO [dbo].[DealCommissionParams]
SELECT d.DealId, d.OvernightFees, d.Commission, d.OnBehalfFee, d.Taxes FROM [dbo].[Deals] d;

ALTER TABLE [dbo].[Deals] DROP COLUMN [OvernightFees];
ALTER TABLE [dbo].[Deals] DROP COLUMN [Commission];
ALTER TABLE [dbo].[Deals] DROP COLUMN [OnBehalfFee];
ALTER TABLE [dbo].[Deals] DROP COLUMN [Taxes];
````

## v1.15.3 - Improvements
### Tasks

LT-1711: add new flag to AccountHistory API
LT-1705: multiple consumers for history broker
LT-1726: Add validation in Accounts Management service

## v1.15.1 - Performance improvements
### Tasks
LT-1579: [Performance testing] Deadlock issue

### Deployment
#### Alters must be applied
````sql
DROP TRIGGER [dbo].[T_InsertAccountTransaction];
````

## v1.15.0 - License, account reset, bugfixes
### Tasks
MTC-837: CR Demo mode is not clearing history
LT-1541: Update licenses in all service to be up to latest requirements
MTC-848: AccountHistoryBroker: fail on account creation event

## v1.14.2 - Secure API, improvements
### Tasks
MTC-732: Broker message handling strategy must be durable
MTC-809: Secure all "visible" endpoints in mt-core
MTC-774: Improve AccountBalanceHistoryApi endpoints

### Service deployment
Add new section to the settings
:exclamation: Optional. Should be used only if all the clients of the service are already updated and use this API key
```json
"MarginTradingAccountManagementServiceClient": 
  {
    "ApiKey": "account management secret key"
  }
```

Add new property ApiKey to MarginTradingSettingsServiceClient (optional, if settings service does not use API key):
```json
"MarginTradingSettingsServiceClient": 
  {
    "ServiceUrl": "settings service url",
    "ApiKey": "settings service secret key"
  }
```

### Broker deployment
Broker should be redeployed in the following way:
1. Make sure the broker queue (dev.AccountsManagement.events.exchange.MarginTrading.AccountsManagement.AccountHistoryBroker.DefaultEnv) is empty.
2. Stop the broker.
3. Go to RabbitMq dashboard and delete the queue.
4. Start new version of broker.

## v1.14.1 - Extend WithdrawalFailedEvent
### Tasks

MTC-787: Add AccountId, ClientId and Amount to WithdrawalFailedEvent

## v1.14.0 - RabbitMQ logs
### Tasks:
MTC-703: RabbitMQ logs
MTC-718: Use distinct account IDs for delete

## v1.13.1 - Delete accounts feature, improvements
### Tasks

MTC-594: "Delete" accounts feature
MTC-673: Do not create balance transactions with 0 change amount
MTC-597: Get account balance on a particular date

### Deployment

#### Apply SQL scripts

ALTER TABLE dbo.MarginTradingAccounts
ADD [IsDeleted] [bit] NOT NULL DEFAULT (0);
  
IF EXISTS(SELECT * FROM sys.indexes WHERE name='IX_Accounts_Client')
  DROP INDEX IX_Accounts_Client ON dbo.MarginTradingAccounts;

IF EXISTS(SELECT * FROM sys.indexes WHERE name='IX_MarginTradingAccounts')
  DROP INDEX IX_MarginTradingAccounts ON dbo.MarginTradingAccounts;

CREATE INDEX IX_MarginTradingAccounts ON dbo.MarginTradingAccounts (ClientId, IsDeleted);

## v1.12.4 - Withdrawal fix for new accounts
## Tasks
MTC-609: fix broken TemporaryCapital serialization on account creation

## Release notes
The following script should be applied:
UPDATE MarginTradingAccounts
SET TemporaryCapital = '[]'
WHERE TemporaryCapital = 'System.Collections.Generic.List`1[MarginTrading.AccountsManagement.InternalModels.TemporaryCapital]'

## v1.12.3 - SQL script fix
### Tasks
MTC-598 : Accounts service is not creating the column TemporaryCapital

### Release notes
Table MarginTradingAccounts should be removed before release, or the following script should be applied: 
ALTER TABLE MarginTradingAccounts
ADD TemporaryCapital NVARCHAR (MAX) NOT NULL DEFAULT ('[]');
GO

## v1.12.2 - Roundings, activities, improvements
### Tasks 

MTC-463 : Gracefully handle Refit exceptions in all services
MTC-552 : Finish trading activities implementation
MTC-574 : Round cash values to 2 decimals

## v1.11.1 - Temporary Capital
### Implemented features

MTC-297 : Temporary Capital
MTC-532 : Return date of transaction if TradingDate is empty

### Deployment comments

#### SQL Alter must be applied:

ALTER TABLE MarginTradingAccounts
ADD TemporaryCapital NVARCHAR (MAX) NOT NULL DEFAULT ('[]');
GO

CREATE INDEX IX_AccountHistory_Base
ON AccountHistory (Id, AccountId, ChangeTimestamp, EventSourceId, ReasonType)
WITH (DROP_EXISTING = ON);
GO

## v1.10.2 - Optional balance change reason
Make balance change reason optional (MTC-525)

## v1.10.1 - Removed validation on disable trading
Removed orders and positions count validation on account disabling (MTC-520)

## v1.10.0 - Improvements, bugfixes
Features:
- Negative balance notification to be postponed until liquidation finished (MTC-424)

Bugfixes:
- Send balance change info for default balance and reset action (MTC-460)

## v1.9.3 - Bugfixes
### Bugfixes

- Fixed Serilog text logs (MTC-461)
- LogsConnString and StorageType are made Optional (MTC-446)

## v1.9.1 - Deployment and maintenance improvements
Introduced:
- Text logs
- Kestrel configurations (see README)
- Default withdrawals is enabled setting

New settings:
"UseSerilog": true,
"Behavior": {
    "BalanceResetIsEnabled": false,
    "DefaultWithdrawalIsEnabled": true
}

## v1.8.1 - Improvements, bugfixes, new settings
Change DB section in Account Management settings: 

```
"Db": {
      "StorageMode": "SqlServer",
      "ConnectionString": "${SqlConnectionString}",
      "LogsConnString": "${SqlConnectionString}"
    }
```

New Account History Broker settings format 

```
{
  "MtBrokerSettings": 
  {
    "MtRabbitMqConnString": "${MtRabbitMqConnectionString}",
    "Db": 
    {
      "StorageMode": "SqlServer",
      "ConnString": "${SqlConnectionString}"
    },
    "RabbitMqQueues": 
    {
      "AccountHistory": 
      {
        "ExchangeName": "dev.AccountsManagement.events.exchange"
      }
    }
  },
  "MtBrokersLogs": 
  {
    "LogsConnString": "${SqlConnectionString}",
    "StorageMode": "SqlServer"
  }
}
```

Execute SQL: 

```
ALTER TABLE [dbo].[OperationExecutionInfo]
ALTER COLUMN [Id] [NVARCHAR] (128) NOT NULL

CREATE INDEX IX_AccountHistory_Base ON AccountHistory (Id, AccountId, ChangeTimestamp, EventSourceId);
CREATE INDEX IX_Accounts_Client ON MarginTradingAccounts (ClientId);
```

## v1.7.2 - Account trading and withdrawals availability
Following section must be added to settings root:

"MtBackendServiceClient": {
    "ServiceUrl": "PUT URL HERE",
    "ApiKey": "PUT API KEY HERE"
  }

Alters must be applied to the SQL:

DROP TABLE OperationExecutionInfo

ALTER TABLE [dbo].[MarginTradingAccounts]
ADD [IsWithdrawalDisabled] BIT NOT NULL DEFAULT 0

## v1.6.1 - AssetPairID in transaction, improvements

## v1.5.3 - Negative balance protection, improvements, bugfixes
[Configs.zip](https://github.com/lykkecloud/MarginTrading.AccountsManagement/files/2251163/Configs.zip)

New ports:

AccountsManagement (5020)
AccountHistoryBroker (5021)

## v1.3.0 - Account balance change saga, bug fixes
Configuration example:
[accounts.appsettings.json.zip](https://github.com/lykkecloud/MarginTrading.AccountsManagement/files/2162450/accounts.appsettings.json.zip)


## 1.2.0 - Withdrawals, Deposits, Cash movements history, Persistence of all data into SQL
[ConfigurationFilesExamples.zip](https://github.com/lykkecloud/MarginTrading.AccountsManagement/files/2118573/ConfigurationFilesExamples.zip)
