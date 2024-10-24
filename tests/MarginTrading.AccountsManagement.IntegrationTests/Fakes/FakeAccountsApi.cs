// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

using Lykke.Contracts.Responses;

using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Account;

using Refit;

namespace MarginTrading.AccountsManagement.IntegrationTests.Fakes
{
    internal sealed class FakeAccountsApi: IAccountsApi
    {
        public Task<List<AccountStatContract>> GetAllAccountStats()
        {
            throw new System.NotImplementedException();
        }

        public Task<PaginatedResponse<AccountStatContract>> GetAllAccountStatsByPages(int? skip = null, int? take = null)
        {
            throw new System.NotImplementedException();
        }
        public Task<List<string>> GetAllAccountIdsFiltered(ActiveAccountsRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<AccountStatContract> GetAccountStats(string accountId)
        {
            throw new System.NotImplementedException();
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