-- Copyright (c) 2021 BNP Paribas Arbitrage. All rights reserved.

IF NOT EXISTS (
    SELECT c.column_id FROM sys.columns AS c
    INNER JOIN sys.tables AS t ON t.object_id = c.object_id
    INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
    WHERE t.object_id = object_id('MarginTradingAccounts') AND s.name = 'dbo' AND c.name = 'CreationTimestamp')
BEGIN
ALTER TABLE [dbo].[MarginTradingAccounts] ADD CreationTimestamp [DateTime] NULL;
exec('UPDATE [dbo].[MarginTradingAccounts] SET CreationTimestamp = (SELECT TOP 1 Timestamp FROM dbo.Activities WHERE Activities.AccountId = MarginTradingAccounts.Id order by [Timestamp])')
exec('UPDATE [dbo].[MarginTradingAccounts] SET CreationTimestamp = ModificationTimestamp WHERE CreationTimestamp IS NULL')
ALTER TABLE [dbo].[MarginTradingAccounts] ALTER COLUMN CreationTimestamp [DateTime] NOT NULL;
END