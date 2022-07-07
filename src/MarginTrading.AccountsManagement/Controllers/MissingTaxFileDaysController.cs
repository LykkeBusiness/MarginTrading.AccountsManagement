// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;
using System.Threading.Tasks;
using MarginTrading.AccountsManagement.Contracts;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.AccountsManagement.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace MarginTrading.AccountsManagement.Controllers
{
    [Microsoft.AspNetCore.Authorization.Authorize]
    [Route("api/missing-taxfile-days")]
    public class MissingTaxFileDaysController : ControllerBase, IMissingTaxFileDaysApi
    {
        private readonly IEodTaxFileMissingRepository _taxFileMissingRepository;

        public MissingTaxFileDaysController(IEodTaxFileMissingRepository taxFileMissingRepository)
        {
            _taxFileMissingRepository = taxFileMissingRepository;
        }
        
        [HttpGet]
        public async Task<MissingTaxFileDays> GetMissingTaxDays()
        {
            var taxFileMissingDays = await _taxFileMissingRepository.GetAllDaysAsync();
            return new MissingTaxFileDays()
            {
                MissingDays = taxFileMissingDays.ToList(),
            };
        }
    }
}