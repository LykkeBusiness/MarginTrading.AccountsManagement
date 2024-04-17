IF NOT EXISTS(SELECT 'X'
              FROM INFORMATION_SCHEMA.TABLES
              WHERE TABLE_NAME = 'AccountHistory'
                AND TABLE_SCHEMA = 'dbo')
    BEGIN
        CREATE TABLE [dbo].[AccountHistory]
        (
            [Oid]                   [bigint]        NOT NULL IDENTITY (1,1) PRIMARY KEY,
            [Id]                    [nvarchar](128) NOT NULL UNIQUE,
            [AccountId]             [nvarchar](64)  NOT NULL,
            [ChangeTimestamp]       [datetime]      NOT NULL,
            [ClientId]              [nvarchar](64)  NOT NULL,
            [ChangeAmount]          decimal(24, 12) NOT NULL,
            [Balance]               decimal(24, 12) NOT NULL,
            [WithdrawTransferLimit] decimal(24, 12) NOT NULL,
            [Comment]               [nvarchar](MAX) NULL,
            [ReasonType]            [nvarchar](64)  NULL,
            [EventSourceId]         [nvarchar](128) NULL,
            [LegalEntity]           [nvarchar](64)  NULL,
            [AuditLog]              [nvarchar](MAX) NULL,
            [Instrument]            [nvarchar](64)  NULL,
            [TradingDate]           [datetime]      NULL,
            INDEX IX_AccountHistory_Base (Id, AccountId, ChangeTimestamp, EventSourceId, ReasonType),
            INDEX IX_AccountHistory_ReasonType_EventSourceId (ReasonType, EventSourceId)
        );
    END;

IF NOT EXISTS (
  SELECT * 
  FROM   sys.columns 
  WHERE  object_id = OBJECT_ID(N'[dbo].[AccountHistory]') 
         AND name = 'CorrelationId'
)
BEGIN
ALTER TABLE [dbo].[AccountHistory]
    ADD CorrelationId nvarchar(250) NULL;
END

IF NOT EXISTS(
        SELECT 'X'
        FROM sys.indexes
        WHERE name = 'IX_AccountHistory_AccountId_ChangeTimestamp'
          AND object_id = OBJECT_ID('dbo.AccountHistory'))
    BEGIN
        CREATE INDEX IX_AccountHistory_AccountId_ChangeTimestamp
            ON AccountHistory (AccountId, ChangeTimestamp) INCLUDE
            (Id,
             ClientId, 
             ChangeAmount, 
             Balance, 
             WithdrawTransferLimit, 
             Comment, 
             ReasonType, 
             EventSourceId, 
             LegalEntity, 
             AuditLog, 
             Instrument, 
             TradingDate)
    END;
    
-- for nvarchar columns the real length is this value divided by 2
-- 128 means it's [nvarchar](64)
if COL_LENGTH('[dbo].[AccountHistory]', 'Instrument') = 128
BEGIN
ALTER TABLE AccountHistory
ALTER COLUMN Instrument NVARCHAR(100)
END;