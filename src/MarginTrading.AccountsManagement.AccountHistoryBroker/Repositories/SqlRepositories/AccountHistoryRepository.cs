// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Microsoft.Extensions.Logging;
using Dapper;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Models;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Services;
using MarginTrading.AccountsManagement.Dal.Common;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker.Repositories.SqlRepositories
{
    public class AccountHistoryRepository : IAccountHistoryRepository
    {
        private readonly Settings _settings;
        private readonly IConvertService _convertService;
        private readonly ILogger _logger;

        private static readonly string GetColumns = SqlUtilities.GetColumns<IAccountHistory>();
        private static readonly string GetFields = SqlUtilities.GetFields<IAccountHistory>();

        public AccountHistoryRepository(Settings settings,
            IConvertService convertService,
            ILogger<AccountHistoryRepository> logger)
        {
            _settings = settings;
            _convertService = convertService;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _settings.Db.ConnString.InitializeSqlObject("dbo.AccountHistory.sql", _logger);
            _settings.Db.ConnString.InitializeSqlObject("dbo.UpdateDealCommissionParamsOnAccountHistory.sql", _logger);
        }

        public async Task InsertAsync(IAccountHistory obj)
        {
            var entity = _convertService.Convert<AccountHistoryEntity>(obj);

            await using (var conn = new SqlConnection(_settings.Db.ConnString))
            {
                if (conn.State == ConnectionState.Closed)
                {
                    await conn.OpenAsync();
                }
                var transaction = conn.BeginTransaction(IsolationLevel.Serializable);
                
                try
                {
                    await conn.ExecuteAsync($"INSERT INTO [dbo].[AccountHistory] ({GetColumns}) VALUES ({GetFields})",
                        entity,
                        transaction);
                    
                    transaction.Commit();
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    
                    var msg = $"Error {ex.Message} \n" +
                           "Entity <AccountHistoryEntity>: \n" +
                           entity.ToJson();
                    _logger.LogError(ex, msg);

                    throw;
                }
            }
            
            await Task.Run(async () =>
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(obj.EventSourceId) && new[]
                    {
                        AccountBalanceChangeReasonType.Commission,
                        AccountBalanceChangeReasonType.OnBehalf,
                        AccountBalanceChangeReasonType.Tax,
                    }.Contains(obj.ReasonType))
                    {
                        throw new Exception($"EventSourceId was null, with reason type {obj.ReasonType}");
                    }

                    await using var conn = new SqlConnection(_settings.Db.ConnString);
                    await conn.ExecuteAsync("[dbo].[UpdateDealCommissionParamsOnAccountHistory]",
                        new
                        {
                            ChangeAmount = obj.ChangeAmount,
                            ReasonType = obj.ReasonType.ToString(),
                            EventSourceId = obj.EventSourceId,
                        },
                        commandType: CommandType.StoredProcedure);
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception,
                        "Failed to calculate commissions for eventSourceId {EventSourceId} with reasonType {ReasonType}, skipping",
                        obj.EventSourceId,
                        obj.ReasonType);
                }        
            });
        }
    }
}
