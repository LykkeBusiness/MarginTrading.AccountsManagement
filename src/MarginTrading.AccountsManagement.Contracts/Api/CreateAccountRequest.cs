// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;

namespace MarginTrading.AccountsManagement.Contracts.Api
{
    public class CreateAccountRequest
    {
        [NotNull]
        public string ClientId { get; set; }
        [NotNull]
        public string AccountId { get; set; }
        [NotNull]
        public string BaseAssetId { get; set; }
        [CanBeNull]
        public string TradingConditionId{ get; set; }
        [CanBeNull]
        public string AccountName { get; set; }
        [CanBeNull]
        public string UserId { get; set; }
        [NotNull]
        public string ReferenceAccount { get; set; }
    }
}