using System.Collections.Generic;
using System.Threading.Tasks;

using Lykke.Snow.Mdm.Contracts.Api;
using Lykke.Snow.Mdm.Contracts.Models.Contracts;
using Lykke.Snow.Mdm.Contracts.Models.Requests;
using Lykke.Snow.Mdm.Contracts.Models.Responses;

namespace MarginTrading.AccountsManagement.Tests.Fakes
{
    internal sealed class FakeBrokerSettingsApi : IBrokerSettingsApi
    {
        public async Task<GetBrokerSettingsByIdResponse> GetByIdAsync(string id)
        {
            return new GetBrokerSettingsByIdResponse
            {
                ErrorCode = BrokerSettingsErrorCodesContract.None,
                BrokerSettings = new BrokerSettingsContract { BrokerId = "Broker1" }
            };
        }

        public async Task<GetAllBrokerSettingsIdsResponse> GetAllIdsAsync()
        {
            throw new System.NotImplementedException();
        }

        public async Task<IReadOnlyList<SupportDataSearchBrokerSettingsContract>> GetAllForSupportDataSearchAsync()
        {
            throw new System.NotImplementedException();
        }

        public async Task<ErrorCodeResponse<BrokerSettingsErrorCodesContract>> AddBrokerSettingsAsync(AddBrokerSettingsRequest request)
        {
            throw new System.NotImplementedException();
        }

        public async Task<ErrorCodeResponse<BrokerSettingsErrorCodesContract>> UpdateBrokerSettingsAsync(UpdateBrokerSettingsRequest request, string id)
        {
            throw new System.NotImplementedException();
        }

        public async Task<ErrorCodeResponse<BrokerSettingsErrorCodesContract>> DeleteBrokerSettingsAsync(string id, string username)
        {
            throw new System.NotImplementedException();
        }

        public async Task<GetBrokerSettingsScheduleResponse> GetScheduleInfoByIdAsync(string id)
        {
            throw new System.NotImplementedException();
        }
    }
}