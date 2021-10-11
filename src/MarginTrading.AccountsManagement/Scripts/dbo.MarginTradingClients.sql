-- Copyright (c) 2021 BNP Paribas Arbitrage. All rights reserved.

-- Create the table if the table doesn't already exist
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE [name] = 'MarginTradingClients' AND schema_id = schema_id('dbo'))
BEGIN
    CREATE TABLE [dbo].[MarginTradingClients](
        [Id] [nvarchar] (64) NOT NULL PRIMARY KEY,
        [TradingConditionId] [nvarchar] (64) NOT NULL);
END