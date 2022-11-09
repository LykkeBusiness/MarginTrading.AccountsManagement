// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.RecoveryTool.Model;
using MarginTrading.AccountsManagement.Repositories;

namespace MarginTrading.AccountsManagement.RecoveryTool.Mappers

{
    public class AccountChangedEventMapper
    {
        private readonly IAccountsRepository _accountsRepository;
        private readonly IConvertService _convertService;

        public AccountChangedEventMapper(IAccountsRepository accountsRepository, IConvertService convertService)
        {
            _accountsRepository = accountsRepository;
            _convertService = convertService;
        }

        public async Task<AccountChangedEvent> Map(UpdateBalanceInternalCommand command)
        {
            var accountId = command.AccountId;

            var account = await _accountsRepository.GetAsync(accountId);

            var change = new AccountBalanceChangeContract(
                command.OperationId,
                account.ModificationTimestamp,
                account.Id,
                account.ClientId,
                command.AmountDelta,
                account.Balance,
                account.WithdrawTransferLimit,
                command.Comment,
                Convert(command.ChangeReasonType),
                command.EventSourceId,
                account.LegalEntity,
                command.AuditLog,
                command.AssetPairId,
                command.TradingDay);

            var convertedAccount = Convert(account);

            var @event = new AccountChangedEvent(
                change.ChangeTimestamp,
                command.Source,
                convertedAccount,
                AccountChangedEventTypeContract.BalanceUpdated,
                change,
                command.OperationId);

            return @event;
        }

        private AccountBalanceChangeReasonTypeContract Convert(AccountBalanceChangeReasonType reasonType)
        {
            return _convertService.Convert<AccountBalanceChangeReasonTypeContract>(reasonType);
        }

        private AccountContract Convert(IAccount account)
        {
            return _convertService.Convert<AccountContract>(account);
        }
    }
}