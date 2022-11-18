// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Data;
using System.Threading.Tasks;

using Lykke.Snow.Common;
using Lykke.Snow.Common.Model;

using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Settings;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    internal class AccountHistoryRepository: SqlRepositoryBase, IAccountHistoryRepository
    {
        private const string _calculateLossPercentageProcName = "dbo.CalculateLossPercentage";
        
        public AccountHistoryRepository(
            AccountManagementSettings settings,
            ILogger<AccountHistoryRepository> logger) : base(settings.Db.ConnectionString, logger)
        {
        }

        public void Initialize()
        {
            ExecCreateOrAlter($"{_calculateLossPercentageProcName}.sql");
        }

        public async Task<IAccountHistoryLossPercentage> CalculateLossPercentageAsync(DateTime from)
        {
            return await GetAsync(_calculateLossPercentageProcName, new[] { new SqlParameter("@From", SqlDbType.DateTime) { Value = from } }, Map);
        }
        
        private AccountHistoryLossPercentageEntity Map(SqlDataReader reader)
        {
            return new AccountHistoryLossPercentageEntity
            {
                ClientNumber = Convert.ToInt32(reader[nameof(AccountHistoryLossPercentageEntity.ClientNumber)]),
                LooserNumber = Convert.ToInt32(reader[nameof(AccountHistoryLossPercentageEntity.LooserNumber)])
            };
        }
    }
}