// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using JetBrains.Annotations;
using Lykke.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Workflow.NegativeProtection.Commands;

namespace MarginTrading.AccountsManagement.Workflow.NegativeProtection
{
    [UsedImplicitly]
    internal class NegativeProtectionCommandsHandler
    {
        private readonly AccountManagementSettings _accountManagementSettings;

        public NegativeProtectionCommandsHandler(
            AccountManagementSettings accountManagementSettings)
        {
            _accountManagementSettings = accountManagementSettings;
        }

        [UsedImplicitly]
        public void Handle(NotifyNegativeProtectionInternalCommand command, IEventPublisher publisher)
        {
            publisher.PublishEvent(new NegativeProtectionEvent(
                new OperationId(),
                command.CorrelationId,
                command.CausationId,
                command.EventTimestamp,
                command.ClientId,
                command.AccountId,
                command.Amount,
                _accountManagementSettings.NegativeProtectionAutoCompensation
            ));
        }
    }
}