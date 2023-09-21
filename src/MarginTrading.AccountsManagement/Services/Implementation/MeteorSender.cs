// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Threading.Tasks;

using Meteor.Client;
using Meteor.Client.Models;

using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Services.Implementation
{
    public class MeteorSender : IMeteorSender
    {
        private readonly IMeteorClient _meteorClient;
        private readonly ILogger<MeteorSender> _logger;

        public MeteorSender(IMeteorClient meteorClient,
            ILogger<MeteorSender> logger)
        {
            _meteorClient = meteorClient;
            _logger = logger;
        }
        
        public async Task Send871mWarningConfirmed(string accountId)
        {  
            var response = await _meteorClient.SendMessage(new SystemMessageRequestModel
            {
                Recipients = accountId,
                Event = MessageEventType.Warning871mConfirmed,
                IsImportant = true,
                MarkAsRead = true
            });
                
            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                _logger.LogError($"Could not send message for 871m warning confirmed. Account id: {accountId}. Status code: {response.StatusCode}, response content: {content}");
            }
        }
    }
}