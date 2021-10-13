-- Copyright (c) 2021 BNP Paribas Arbitrage. All rights reserved.

-- exec sp_helptext [dbo.DeleteAccountData]
-- exec sp_help [dbo.DeleteAccountData]
-- exec [dbo].[DeleteAccountData] <accountId> 

CREATE OR ALTER PROCEDURE [dbo].[DeleteAccountData] (
    @AccountId NVARCHAR(128))
AS
BEGIN
    SET NOCOUNT ON;
    BEGIN TRANSACTION

    DELETE [dbo].[AccountHistory] WHERE AccountId = @AccountId;
    DELETE [dbo].[OrdersHistory] WHERE AccountId = @AccountId;
    DELETE [dbo].[PositionsHistory] WHERE AccountId = @AccountId;
    DELETE [dbo].[Trades] WHERE AccountId = @AccountId;
    DELETE [dbo].[Deals] WHERE AccountId = @AccountId;
    DELETE [dbo].[Activities] WHERE AccountId = @AccountId;

    COMMIT TRANSACTION
END;