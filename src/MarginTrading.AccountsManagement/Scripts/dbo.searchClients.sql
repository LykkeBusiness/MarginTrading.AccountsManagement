-- Copyright (c) 2021 BNP Paribas Arbitrage. All rights reserved.

-- exec sp_helptext [dbo.searchClients]
-- exec sp_help [dbo.searchClients]
-- exec [dbo].[searchClients] 'query', 0/1, 0, 20


CREATE OR ALTER PROCEDURE [dbo].[searchClients] (
    @Query NVARCHAR(128),
    @ByClient BIT = 1,
    @Skip INT = 0,
    @Take INT = 20
)
AS
BEGIN
    SET NOCOUNT ON;

    WITH clients AS (
        SELECT ctc.Id,
               ctc.TradingConditionId,
               COALESCE(ctc.AccountNameCommaSeparatedList, ctc.AccountIdCommaSeparatedList) as AccountIdentityCommaSeparatedList
        FROM (
                 SELECT c.Id,
                        c.TradingConditionId,
                        STUFF((SELECT ',' + acc.AccountName
                               FROM MarginTradingAccounts acc
                               WHERE acc.ClientId = c.Id
                            FOR XML PATH ('')), 1, 1, '')                    AS AccountNameCommaSeparatedList,
                        STUFF((SELECT ',' + acc.Id
                               FROM MarginTradingAccounts acc
                               WHERE acc.ClientId = c.Id
                            FOR XML PATH ('')),
                              1, 1, '') AS AccountIdCommaSeparatedList
                 FROM MarginTradingClients c
                          INNER JOIN
                      MarginTradingAccounts a on c.Id = a.ClientId
                 GROUP by c.Id, c.TradingConditionId
             ) ctc
        WHERE
            (@ByClient = 0 AND COALESCE(ctc.AccountNameCommaSeparatedList, ctc.AccountIdCommaSeparatedList) LIKE CONCAT('%', @query, '%')) OR
            (@ByClient = 1 AND ctc.Id LIKE CONCAT('%', @query, '%'))
    ),
    rowsCount as (
        SELECT 
            COUNT(*) as Total 
        FROM 
             clients
    )
    SELECT
        Id,
        TradingConditionId,
        AccountIdentityCommaSeparatedList,
        CONVERT(INT, rowsCount.Total) AS TotalRows
    FROM
         clients, rowsCount
    ORDER BY 
        TradingConditionId
    OFFSET @Skip ROWS FETCH NEXT @Take ROWS ONLY
END