-- Copyright (c) 2021 BNP Paribas Arbitrage. All rights reserved.

-- Create the table if the table doesn't already exist
IF NOT EXISTS (SELECT 1 FROM sys.objects WHERE [name] = 'MarginTradingClients' AND schema_id = schema_id('dbo'))
BEGIN
    CREATE TABLE [dbo].[MarginTradingClients](
        [Id] [nvarchar] (64) NOT NULL PRIMARY KEY,
        [TradingConditionId] [nvarchar] (64) NOT NULL);
END

-- Add UserId column if it has not been added yet
IF NOT EXISTS (
    SELECT c.column_id FROM sys.columns AS c
    INNER JOIN sys.tables AS t ON t.object_id = c.object_id
    INNER JOIN sys.schemas AS s ON s.schema_id = t.schema_id
    WHERE t.object_id = object_id('MarginTradingClients') AND s.name = 'dbo' AND c.name = 'UserId')
BEGIN
    ALTER TABLE [dbo].[MarginTradingClients] ADD UserId nvarchar(128) NULL;

    -- Data migration
    UPDATE c
        SET c.UserId = u.Username
    FROM 
        [dbo].[MarginTradingClients] c
    INNER JOIN 
        [bouncer].[AspNetUsers] u ON c.Id = u.Id
END