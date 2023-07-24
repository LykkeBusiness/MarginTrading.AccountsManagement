// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Net;
using System.Threading.Tasks;

using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.AccountsManagement.Contracts.Audit;
using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.AccountsManagement.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/audit")]
    public class AuditController : ControllerBase, IAuditApi
    {
        private readonly IAuditService _auditService;
        private readonly IConvertService _convertService;

        public AuditController(IAuditService auditService, IConvertService convertService)
        {
            _auditService = auditService;
            _convertService = convertService;
        }

        /// <summary>
        /// Get audit logs
        /// </summary>
        /// <param name="request"></param>
        /// <param name="skip"></param>
        /// <param name="take"></param>
        /// <returns></returns>
        [HttpGet]
        [ProducesResponseType(typeof(Lykke.Contracts.Responses.PaginatedResponse<AuditContract>), (int)HttpStatusCode.OK)]
        public async Task<Lykke.Contracts.Responses.PaginatedResponse<AuditContract>> GetAuditTrailAsync([FromQuery] GetAuditLogsRequest request, int? skip = null, int? take = null)
        {
            var filter = _convertService.Convert<GetAuditLogsRequest, AuditLogsFilterDto>(request);
            var result = await _auditService.GetAll(filter, skip, take);

            return new Lykke.Contracts.Responses.PaginatedResponse<AuditContract>(
                result.Contents.Select(i => _convertService.Convert<AuditModel, AuditContract>(i)).ToList(),
                result.Start,
                result.Size,
                result.TotalSize
            );
        }
    }
}