// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

namespace MarginTrading.AccountsManagement.Contracts.Api
{
    /// <summary>
    /// Account modification parameters. Only one parameter may be changed in a request.
    /// </summary>
    public class ChangeAccountRequest
    {
        public bool? IsDisabled { get; set; }
        public bool? IsWithdrawalDisabled { get; set; }
    }
}