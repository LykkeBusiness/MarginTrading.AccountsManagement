// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.RecoveryTool.LogParsers;
using MarginTrading.AccountsManagement.RecoveryTool.Mappers;
using MarginTrading.AccountsManagement.RecoveryTool.Model;
using MarginTrading.AccountsManagement.RecoveryTool.Services;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.RecoveryTool
{
    public class App
    {
        private readonly AccountsManagementLogParser _accountsManagementLogParser;
        private readonly UpdateBalanceInternalCommandMapper _updateBalanceInternalCommandMapper;
        private readonly ChangeBalanceCommandMapper _changeBalanceCommandMapper;
        private readonly AccountChangedEventMapper _accountChangedEventMapper;
        private readonly IConfiguration _configuration;
        private readonly Publisher _publisher;
        private readonly ILogger<App> _logger;
        private string[] _queues;
        private readonly Dictionary<string, bool> _commands;

        public App(
            AccountsManagementLogParser accountsManagementLogParser,
            UpdateBalanceInternalCommandMapper updateBalanceInternalCommandMapper,
            ChangeBalanceCommandMapper changeBalanceCommandMapper,
            AccountChangedEventMapper accountChangedEventMapper,
            IConfiguration configuration,
            Publisher publisher,
            ILogger<App> logger)
        {
            _accountsManagementLogParser = accountsManagementLogParser;
            _updateBalanceInternalCommandMapper = updateBalanceInternalCommandMapper;
            _changeBalanceCommandMapper = changeBalanceCommandMapper;
            _accountChangedEventMapper = accountChangedEventMapper;
            _configuration = configuration;
            _publisher = publisher;
            _logger = logger;

            _queues = _configuration.GetSection("Queues").Get<string[]>();
            _commands = _configuration.GetSection("Commands").Get<Dictionary<string, bool>>();
        }

        public async Task ImportFromAccountManagementAsync()
        {
            _logger.LogInformation("Starting to import data from Accounts Management");

            var activityProducerPath = _configuration.GetValue<string>("AccountsManagementLogDirectory");
            var files = GetFiles(activityProducerPath, "accounts management");

            (List<UpdateBalanceInternalCommand> updateBalanceInternalCommands,
                List<ChangeBalanceCommand> changeBalanceCommands) = await ParseCommands(files);

            List<AccountChangedEvent> accountChangedEvents =
                await MapAndFilter(updateBalanceInternalCommands, changeBalanceCommands);

            _logger.LogInformation("{N} total events found", accountChangedEvents.Count);
            accountChangedEvents = accountChangedEvents
                .OrderBy(x => x.ChangeTimestamp)
                .ToList();

            foreach (var queue in _queues)
            {
                _logger.LogInformation("Pushing to queue {Queue}", queue);
                foreach (var @event in accountChangedEvents)
                {
                    _publisher.Publish(@event, queue);
                }
            }

            _logger.LogInformation("Data from Accounts Management imported. Press any button to exit the app.");
            Console.ReadKey();
        }

        private async Task<List<AccountChangedEvent>> MapAndFilter(
            List<UpdateBalanceInternalCommand> updateBalanceInternalCommands,
            List<ChangeBalanceCommand> changeBalanceCommands)
        {
            var accountChangedEvents = new List<AccountChangedEvent>();

            var sendUpdateBalanceInternalCommand = _commands[nameof(UpdateBalanceInternalCommand)];
            if (sendUpdateBalanceInternalCommand)
            {
                foreach (var command in updateBalanceInternalCommands)
                {
                    var @event = await _accountChangedEventMapper.Map(command);
                    accountChangedEvents.Add(@event);
                }
            }
            else
            {
                _logger.LogInformation("{Command} is disabled and will not be sent",
                    nameof(UpdateBalanceInternalCommand));
            }

            var sendChangeBalanceCommand = _commands[nameof(ChangeBalanceCommand)];
            if (sendChangeBalanceCommand)
            {
                foreach (var command in changeBalanceCommands)
                {
                    var mappedCommand = _updateBalanceInternalCommandMapper.Map(command);
                    var @event = await _accountChangedEventMapper.Map(mappedCommand);
                    accountChangedEvents.Add(@event);
                }
            }
            else
            {
                _logger.LogInformation("{Command} is disabled and will not be sent", nameof(ChangeBalanceCommand));
            }

            return accountChangedEvents;
        }

        private async
            Task<(List<UpdateBalanceInternalCommand> updateBalanceInternalCommands, List<ChangeBalanceCommand>
                changeBalanceCommands)> ParseCommands(List<string> files)
        {
            var updateBalanceInternalCommands = new List<UpdateBalanceInternalCommand>();
            var changeBalanceCommands = new List<ChangeBalanceCommand>();

            foreach (var file in files)
            {
                _logger.LogInformation("Starting to parse {File}", file);
                var domainEvents = _accountsManagementLogParser.Parse(await File.ReadAllTextAsync(file));

                updateBalanceInternalCommands.AddRange(domainEvents
                    .Where(x => x.Type == EventType.UpdateBalanceInternalCommand)
                    .Select(x => _updateBalanceInternalCommandMapper.Map(x))
                );

                changeBalanceCommands.AddRange(domainEvents
                    .Where(x => x.Type == EventType.ChangeBalanceCommand)
                    .Select(x => _changeBalanceCommandMapper.Map(x))
                );

                _logger.LogInformation("{N} change balance commands found", changeBalanceCommands.Count);

                _logger.LogInformation("File {File} processed", file);
            }

            changeBalanceCommands = changeBalanceCommands
                .Where(x => updateBalanceInternalCommands
                    .All(i => i.OperationId != x.OperationId))
                .ToList();

            _logger.LogInformation(
                "{N} change balance commands remains after filtering UpdateBalanceInternalCommand duplicates",
                changeBalanceCommands.Count);
            return (updateBalanceInternalCommands, changeBalanceCommands);
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
}