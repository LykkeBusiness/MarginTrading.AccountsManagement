namespace MarginTrading.AccountsManagement.InternalModels.Interfaces
{
    public interface IClient
    {
        string Id { get;  }
        string TradingConditionId { get;  }
        string UserId { get; }
    }
}
