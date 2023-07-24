using System.Collections.Generic;
using System.Threading.Tasks;

using Lykke.Contracts.Responses;
using Lykke.Snow.Mdm.Contracts.Api;
using Lykke.Snow.Mdm.Contracts.Models.Contracts;
using Lykke.Snow.Mdm.Contracts.Models.Requests;
using Lykke.Snow.Mdm.Contracts.Models.Responses;

namespace MarginTrading.AccountsManagement.Tests.Fakes
{
    internal sealed class FakeBrokerSettingsApi : IBrokerSettingsApi
    {
        public Task<GetBrokerSettingsByIdResponse> GetByIdAsync(string id)
        {
            return Task.FromResult(new GetBrokerSettingsByIdResponse
            {
                ErrorCode = BrokerSettingsErrorCodesContract.None,
                BrokerSettings = new BrokerSettingsContract { BrokerId = "Broker1" }
            });
        }

        public Task<GetAllBrokerSettingsIdsResponse> GetAllIdsAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<IReadOnlyList<SupportDataSearchBrokerSettingsContract>> GetAllForSupportDataSearchAsync()
        {
            throw new System.NotImplementedException();
        }

        public Task<ErrorCodeResponse<BrokerSettingsErrorCodesContract>> AddBrokerSettingsAsync(AddBrokerSettingsRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<ErrorCodeResponse<BrokerSettingsErrorCodesContract>> UpdateBrokerSettingsAsync(UpdateBrokerSettingsRequest request, string id)
        {
            throw new System.NotImplementedException();
        }

        public Task<ErrorCodeResponse<BrokerSettingsErrorCodesContract>> DeleteBrokerSettingsAsync(string id, string username)
        {
            throw new System.NotImplementedException();
        }

        public Task<GetBrokerSettingsScheduleResponse> GetScheduleInfoByIdAsync(string id)
        {
            throw new System.NotImplementedException();
        }

        public Task<ErrorCodeResponse<BrokerChannelErrorCodesContract>> AddOrUpdateBrokerChannelAsync(string id, BrokerChannelType? channelType, string username,
            AddOrUpdateBrokerChannelRequest request)
        {
            throw new System.NotImplementedException();
        }

        public Task<ErrorCodeResponse<BrokerChannelErrorCodesContract>> DeleteBrokerChannelAsync(string id, BrokerChannelType? channelType, string username)
        {
            throw new System.NotImplementedException();
        }

        public Task<GetBrokerChannelsByIdResponse> ListBrokerChannelsAsync(string id)
        {
            throw new System.NotImplementedException();
        }
    }
}