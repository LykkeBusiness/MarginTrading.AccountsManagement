// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Models;
using Refit;

namespace MarginTrading.AccountsManagement.Contracts
{
    [PublicAPI]
    public interface IMissingTaxFileDaysApi
    {
        [Get("/api/missing-taxfile-days")]
        Task<MissingTaxFileDays> GetMissingTaxDays();
    }
}