// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

using Lykke.Contracts.Responses;

using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Orders;
using MarginTrading.Backend.Contracts.Positions;

namespace MarginTrading.AccountsManagement.IntegrationTests.Fakes
{
    internal sealed class FakePositionsApi : IPositionsApi 
    {
        public Task<PositionCloseResponse> CloseAsync(string positionId, PositionCloseRequest request = null, string accountId = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<PositionsGroupCloseResponse> CloseGroupAsync(string assetPairId = null,
            string accountId = null,
            PositionDirectionContract? direction = null,
            PositionCloseRequest request = null)
        {
            throw new System.NotImplementedException();
        }

        public Task<OpenPositionContract> GetAsync(string positionId)
        {
            throw new System.NotImplementedException();
        }

        public Task<List<OpenPositionContract>> ListAsync(string accountId = null, string assetPairId = null)
        {
            return Task.FromResult(new List<OpenPositionContract>());
        }

        public Task<PaginatedResponse<OpenPositionContract>> ListAsyncByPages(string accountId = null, string assetPairId = null, int? skip = null, int? take = null)
        {
            throw new System.NotImplementedException();
        }
    }
}