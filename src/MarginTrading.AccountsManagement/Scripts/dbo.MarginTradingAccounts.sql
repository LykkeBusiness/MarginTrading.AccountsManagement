-- Copyright (c) 2021 BNP Paribas Arbitrage. All rights reserved.

-- Create the table if the table doesn't already exist
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE [name] = 'MarginTradingAccounts' AND schema_id = schema_id('dbo'))
BEGIN
    CREATE TABLE [dbo].[MarginTradingAccounts](
    [Id] [nvarchar] (64) NOT NULL PRIMARY KEY,
    [ClientId] [nvarchar] (64) NOT NULL,
    [BaseAssetId] [nvarchar] (64) NOT NULL,
    [Balance] decimal (24, 12) NOT NULL,
    [WithdrawTransferLimit] decimal (24, 12) NOT NULL,
    [LegalEntity] [nvarchar] (64) NOT NULL,
    [IsDisabled] [bit] NOT NULL,
    [IsWithdrawalDisabled] [bit] NOT NULL,
    [IsDeleted] [bit] NOT NULL,
    [ModificationTimestamp] [DateTime] NOT NULL,
    [TemporaryCapital] [nvarchar] (MAX) NOT NULL,
    [AdditionalInfo] [nvarchar] (MAX) NOT NULL,
    [LastExecutedOperations] [nvarchar] (MAX) NOT NULL,
    [AccountName] [nvarchar] (255)); 
END

-- Create index
IF NOT EXISTS(
	SELECT * FROM sys.indexes 
	WHERE name = 'IX_MarginTradingAccounts'
	AND object_id = OBJECT_ID('dbo.MarginTradingAccounts'))
BEGIN
        CREATE INDEX [IX_MarginTradingAccounts] ON [dbo].[MarginTradingAccounts]
		(
			[ClientId], [IsDeleted]
		)
END

IF OBJECT_ID('dbo.[FK_MarginTradingAccounts_MarginTradingClients]', 'F') IS NULL
BEGIN
    ALTER TABLE [dbo].[MarginTradingAccounts]
        WITH CHECK ADD CONSTRAINT [FK_MarginTradingAccounts_MarginTradingClients] FOREIGN KEY ([ClientId])
        REFERENCES [dbo].[MarginTradingClients] ([Id]);
END;