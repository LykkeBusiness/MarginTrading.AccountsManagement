using System;
using System.Threading.Tasks;

using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.AccountsManagement.IntegrationTests.Infrastructure;

namespace MarginTrading.AccountsManagement.IntegrationTests.WorkflowTests
{
    public static class TestsHelpers
    {
        public const string ClientId = "276e48b9b0cb4760b30ebe6760750c2e";
        public const string AccountId = "ITA20241";

        public static async Task<AccountContract> EnsureAccountState(decimal needBalance = 0)
        {
            var account = await ClientUtil.AccountsApi.GetById(AccountId);
            if (account == null)
            {
                account = (await ClientUtil.AccountsApi.Create(new CreateAccountRequest
                {
                    AccountId = AccountId,
                    BaseAssetId = "EUR"
                })).Content;
            }

            if (account.Balance < needBalance)
            {
                await ChargeManually(needBalance - account.Balance);
                account = await GetAccount();
            }

            if (account.IsDisabled)
            {
                account = await ClientUtil.AccountsApi.Change(AccountId, new ChangeAccountRequest
                {
                    IsDisabled = false,
                });
            }

            return account;
        }

        public static async Task ChargeManually(decimal delta)
        {
            var operationId = await ClientUtil.AccountsApi.BeginChargeManually(AccountId,
                new AccountChargeManuallyRequest
                {
                    OperationId = Guid.NewGuid().ToString("N"),
                    AmountDelta = delta,
                    Reason = "intergational tests"
                });

            await RabbitUtil.WaitForCqrsMessage<AccountChangedEvent>(m => m.OperationId == operationId);
        }

        public static Task<AccountContract> GetAccount()
        {
            return ClientUtil.AccountsApi.GetById(AccountId);
        }
    }
}