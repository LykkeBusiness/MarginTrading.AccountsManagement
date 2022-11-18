-- Copyright (c) 2020 BNP Paribas Arbitrage. All rights reserved.

CREATE OR ALTER PROCEDURE [dbo].[CalculateLossPercentage] (
    @From DATE
)
AS
BEGIN
    SELECT A.NbLoser as LooserNumber, B.NbClient as ClientNumber
    FROM (SELECT count(*) as NbLoser FROM (
        SELECT AccountId, sum(ChangeAmount) as Amount
        FROM AccountHistory WITH (NOLOCK)
        WHERE ReasonType not in ('Deposit','Withdraw')
        AND TradingDate >= @From
        GROUP BY AccountId) as PnLClient
        WHERE Amount < 0) as A,
        (SELECT count(distinct(AccountId)) as NbClient
        FROM AccountHistory WITH (NOLOCK)
        WHERE ReasonType not in ('Deposit','Withdraw')
        AND TradingDate >= @From) as B
END