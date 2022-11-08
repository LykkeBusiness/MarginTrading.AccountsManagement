// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.RecoveryTool.LogParsers;
using MarginTrading.AccountsManagement.RecoveryTool.Mappers;
using MarginTrading.AccountsManagement.RecoveryTool.Model;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.RecoveryTool;

public class App
{
    private readonly AccountsManagementLogParser _accountsManagementLogParser;
    private readonly UpdateBalanceInternalCommandMapper _updateBalanceInternalCommandMapper;
    private readonly AccountChangedEventMapper _accountChangedEventMapper;
    private readonly IConfiguration _configuration;
    private readonly ILogger<App> _logger;

    public App(
        AccountsManagementLogParser accountsManagementLogParser,
        UpdateBalanceInternalCommandMapper updateBalanceInternalCommandMapper,
        AccountChangedEventMapper accountChangedEventMapper,
        IConfiguration configuration,
        ILogger<App> logger)
    {
        _accountsManagementLogParser = accountsManagementLogParser;
        _updateBalanceInternalCommandMapper = updateBalanceInternalCommandMapper;
        _accountChangedEventMapper = accountChangedEventMapper;
        _configuration = configuration;
        _logger = logger;
    }
    
    public async Task ImportFromAccountManagementAsync()
    {
        _logger.LogInformation("Starting to import data from Accounts Management");

        var activityProducerPath = _configuration.GetValue<string>("AccountsManagementLogDirectory");
        var files = GetFiles(activityProducerPath, "accounts management");

        foreach (var file in files)
        {
            _logger.LogInformation("Starting to parse {File}", file);
            var domainEvents = _accountsManagementLogParser.Parse(await File.ReadAllTextAsync(file));

            var commands = domainEvents
                .Select(x => _updateBalanceInternalCommandMapper.Map(x))
                .ToList();

            var accountChangedEvents = new List<AccountChangedEvent>();

            foreach (var command in commands)
            {
                var @event = await _accountChangedEventMapper.Map(command);
                accountChangedEvents.Add(@event);
            }
            
            // TODO: send accountChangedEvents to queues

            _logger.LogInformation("File {File} uploaded", file);
        }

        _logger.LogInformation("Data from Activity Producer imported");
    }
    
    private List<string> GetFiles(string path, string service)
    {
        if (!Directory.Exists(path))
        {
            _logger.LogError("Directory {Path} for service {Service} not found",
                path, service);
            throw new Exception("Check directory configuration: directory not found");
        }

        var files = Directory.EnumerateFiles(path).ToList();
        if (files.Count == 0)
        {
            _logger.LogError("Logfiles not found for service {Service}", service);
            throw new Exception("Check directory configuration: logfiles not found");
        }

        return files;
    }
}