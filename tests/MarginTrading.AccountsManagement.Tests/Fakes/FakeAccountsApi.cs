// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Contracts.Common;

namespace MarginTrading.AccountsManagement.Tests.Fakes
{
    internal sealed class FakeAccountsApi: IAccountsApi
    {
        public Task<List<AccountStatContract>> GetAllAccountStats()
        {
            throw new System.NotImplementedException();
        }

        public Task<PaginatedResponseContract<AccountStatContract>> GetAllAccountStatsByPages(int? skip = null, int? take = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<string>> GetAllAccountIdsFiltered(ActiveAccountsRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<AccountStatContract> GetAccountStats(string accountId)
        {
            return Task.FromResult(new AccountStatContract { AccountId = "fake account id" });
        }

        public Task<AccountCapitalFigures> GetCapitalFigures(string accountId)
        {
            return Task.FromResult(AccountCapitalFigures.Empty);
        }

        public Task ResumeLiquidation(string accountId, string comment)
        {
            throw new System.NotImplementedException();
        }
    }
}