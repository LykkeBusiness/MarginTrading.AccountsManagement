// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;

using Lykke.Contracts.Responses;

namespace MarginTrading.AccountsManagement.Contracts.Models
{
    public class ByPagesAccountBalanceHistoryResponse: PaginatedResponse<AccountBalanceChangeContract>
    {
        public ByPagesAccountBalanceHistoryResponse(IReadOnlyList<AccountBalanceChangeContract> contents, int start, int size, int totalSize, decimal totalAmount)
            : base(contents, start, size, totalSize)
        {
            TotalAmount = totalAmount;
        }

        public decimal TotalAmount { get; private set; }
    }
}