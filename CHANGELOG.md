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
