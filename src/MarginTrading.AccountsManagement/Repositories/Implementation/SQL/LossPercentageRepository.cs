// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Common;

using Dapper;

using Lykke.Logs.MsSql.Extensions;

using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Settings;

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Repositories.Implementation.SQL;

internal class LossPercentageRepository : ILossPercentageRepository
{
    private const string TableName = "LossPercentage";

    private const string CreateTableScript = """
                                             CREATE TABLE [{0}](
                                             [Id] [bigint] NOT NULL IDENTITY (1,1) PRIMARY KEY,
                                             [ClientNumber] [int] NOT NULL,
                                             [LooserNumber] [int] NOT NULL,
                                             [Timestamp] [datetime] NOT NULL)
                                             """;

    private static Type DataType => typeof(ILossPercentage);

    private static PropertyInfo[] InsertColumns =>
        DataType.GetProperties().Where(x => x.Name != nameof(ILossPercentage.Id)).ToArray();

    private static readonly string GetInsertColumns = string.Join(",", InsertColumns.Select(x => x.Name));
    private static readonly string GetInsertFields = string.Join(",", InsertColumns.Select(x => "@" + x.Name));

    private readonly IConvertService _convertService;
    private readonly AccountManagementSettings _settings;
    private readonly ILogger _logger;

    public LossPercentageRepository(
    IConvertService convertService,
    AccountManagementSettings settings,
    ILogger<LossPercentageRepository> logger)
    {
        _convertService = convertService;
        _settings = settings;
        _logger = logger;

        using var conn = new SqlConnection(_settings.Db.ConnectionString);
        conn.CreateTableIfDoesntExists(CreateTableScript, TableName);
    }

    public async Task<ILossPercentage> GetLastAsync()
    {
        await using var conn = new SqlConnection(_settings.Db.ConnectionString);
        return await conn.QuerySingleOrDefaultAsync<LossPercentageEntity>(
            $"SELECT TOP 1 * FROM {TableName} WITH (NOLOCK) ORDER BY {nameof(LossPercentageEntity.Timestamp)} DESC");
    }

    public async Task AddAsync(ILossPercentage value)
    {
        if (value == null)
        {
            throw new ArgumentNullException($"{nameof(value)} cannot be null.");
        }

        var entity = _convertService.Convert<LossPercentageEntity>(value);

        await using var conn = new SqlConnection(_settings.Db.ConnectionString);
        try
        {
            await conn.ExecuteAsync(
                $"insert into {TableName} ({GetInsertColumns}) values ({GetInsertFields})", entity);
        }
        catch (Exception ex)
        {
            var msg = $"""
                       Error {ex.Message}
                       Entity <LossPercentageEntity>: {entity.ToJson()}
                       """;
            _logger.LogWarning(ex, msg);
            throw new Exception(msg);
        }
    }
}