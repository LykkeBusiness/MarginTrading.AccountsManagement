// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;
using JetBrains.Annotations;

using Lykke.Contracts.Responses;

using MarginTrading.AccountsManagement.Contracts.Audit;
using Refit;

namespace MarginTrading.AccountsManagement.Contracts
{
    /// <summary>
    /// Working with audit log
    /// </summary>
    [PublicAPI]
    public interface IAuditApi
    {
        /// <summary>
        /// Get audit logs
        /// </summary>
        /// <param name="request"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        [Get("/api/audit")]
        Task<PaginatedResponse<AuditContract>> GetAuditTrailAsync([Query] GetAuditLogsRequest request, int? skip = null, int? take = null);
    }
}