// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Lykke.Snow.Common;
using Lykke.Snow.Common.Exceptions;
using MarginTrading.AccountsManagement.Dal.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL
{
    public class SqlEodTaxFileMissingRepository: SqlRepositoryBase, IEodTaxFileMissingRepository
    {
        private readonly ILogger _logger;
        
        public SqlEodTaxFileMissingRepository(string connectionString,
            ILogger<SqlEodTaxFileMissingRepository> logger) : base(connectionString, logger)
        {
            _logger = logger;
        }

        public void Initialize()
        {
            ConnectionString.InitializeSqlObject("dbo.TaxFileMissing.sql", _logger);
            ExecCreateOrAlter("dbo.addTaxFileMissing.sql");
            ExecCreateOrAlter("dbo.removeTaxFileMissing.sql");
            ExecCreateOrAlter("dbo.getTaxFileMissing.sql");
        }

        public async Task AddAsync(DateTime tradingDay)
        {
            try
            {
                await ExecuteNonQueryAsync("[dbo].[addTaxFileMissing]",
                    new[]
                    {
                        new SqlParameter("@TradingDate", SqlDbType.DateTime) {Value = tradingDay.Date}
                    });
            }
            catch (InsertionFailedException e)
            {
                _logger.LogWarning(e,
                    "Couldn't insert new value into taxFileMissing table for trading day [{TradingDay}]. Error message = {Message}",
                    tradingDay, e.Message);
            }
        }

        public async Task RemoveAsync(DateTime tradingDay)
        {
            try
            {
                await ExecuteNonQueryAsync("[dbo].[removeTaxFileMissing]",
                    new[] {new SqlParameter("@TradingDate", SqlDbType.DateTime) {Value = tradingDay.Date}});
            }
            catch (InsertionFailedException e)
            {
                _logger.LogWarning(e,
                    "Couldn't delete from taxFileMissing table for trading day [{TradingDay}]. Error message = {Message}",
                    tradingDay, e.Message);
            }
        }

        public async Task<IEnumerable<DateTime>> GetAllDaysAsync()
        {
            try
            {
                return await GetAllAsync("[dbo].[getTaxFileMissing]", null, Map);
            }
            catch (FormatException e)
            {
                _logger.LogError(e,
                    "Couldn't get data from taxFileMissing table. Error message = {Message}",
                    e.Message);
                return Enumerable.Empty<DateTime>();
            }
        }
        
        private DateTime Map(SqlDataReader reader)
        {
            return reader["TradingDate"] as DateTime? ??
                   throw new FormatException("Trading date column value can't be casted to datetime");
        }
    }
}