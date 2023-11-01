// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Account;
using MarginTrading.Backend.Contracts.Common;

namespace MarginTrading.AccountsManagement.Tests.Fakes
{
    public class AlwaysInLiquidationAccountApi : IAccountsApi
    {
        private static AccountStatContract CreateAccountStatContract(string accountId = null) =>
            new AccountStatContract { AccountId = accountId ?? "fake account id", IsInLiquidation = true };

        public Task<List<AccountStatContract>> GetAllAccountStats()
        {
            return Task.FromResult(new List<AccountStatContract> { CreateAccountStatContract() });
        }

        public Task<PaginatedResponseContract<AccountStatContract>> GetAllAccountStatsByPages(int? skip = null, int? take = null)
        {
            var result = new PaginatedResponseContract<AccountStatContract>(
                new List<AccountStatContract>
                {
                    CreateAccountStatContract()
                }, 0, 1, 1);

            return Task.FromResult(result);
        }

        public Task<List<string>> GetAllAccountIdsFiltered(ActiveAccountsRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<AccountStatContract> GetAccountStats(string accountId)
        {
            return Task.FromResult(CreateAccountStatContract(accountId));
        }

        public Task<AccountCapitalFigures> GetCapitalFigures(string accountId)
        {
            throw new System.NotImplementedException();
        }

        public Task ResumeLiquidation(string accountId, string comment)
        {
            throw new System.NotImplementedException();
        }
    }
}