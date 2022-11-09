// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Threading.Tasks;

using Dapper;

using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Settings;

using Microsoft.Data.SqlClient;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    internal class AccountHistoryRepository: IAccountHistoryRepository
    {
        private const string CalculateLossPercentageQuery =
            "SELECT A.NbLoser as LooserNumber, B.NbClient as ClientNumber " +
            "FROM (SELECT count(*) as NbLoser FROM ( "+
            "SELECT AccountId, sum(ChangeAmount) as Amount "+
            "FROM AccountHistory WITH (NOLOCK) "+
            "WHERE ReasonType not in ('Deposit','Withdraw') "+
            "AND TradingDate >= @from "+
            "GROUP BY AccountId) as PnLClient "+
            "WHERE Amount < 0) as A, "+
            "(SELECT count(distinct(AccountId)) as NbClient "+
            "FROM AccountHistory WITH (NOLOCK) "+
            "WHERE ReasonType not in ('Deposit','Withdraw') "+
            "AND TradingDate >= @from) as B";
        
        private readonly AccountManagementSettings _settings;
        
        public AccountHistoryRepository(
            AccountManagementSettings settings)
        {
            _settings = settings;
        }

        public async Task<IAccountHistoryLossPercentage> CalculateLossPercentageAsync(DateTime from)
        {
            await using var conn = new SqlConnection(_settings.Db.ConnectionString);
            return await conn.QuerySingleAsync<AccountHistoryLossPercentageEntity>(
                CalculateLossPercentageQuery, 
                new { from });
        }
    }
}