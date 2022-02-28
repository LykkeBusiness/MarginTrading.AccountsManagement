-- Copyright (c) 2021 BNP Paribas Arbitrage. All rights reserved.

CREATE OR ALTER PROCEDURE [dbo].[getAllClients]
AS
BEGIN
    SELECT c.Id, c.UserId, c.TradingConditionId,
       STUFF((SELECT ',' + acc.Id
              FROM MarginTradingAccounts acc
              WHERE acc.ClientId = c.Id
           FOR XML PATH ('')),
            1, 1, '') AS AccountIdentityCommaSeparatedList
    FROM MarginTradingClients c
    LEFT JOIN MarginTradingAccounts a ON c.Id = a.ClientId
    GROUP BY c.Id, c.UserId, c.TradingConditionId
END