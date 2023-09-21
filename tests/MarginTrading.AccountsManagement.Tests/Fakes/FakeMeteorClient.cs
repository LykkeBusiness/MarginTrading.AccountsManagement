// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

using Meteor.Client;
using Meteor.Client.Models;

namespace MarginTrading.AccountsManagement.Tests.Fakes
{
    internal sealed class FakeMeteorClient: IMeteorClient
    {
        public Task<HttpResponseMessage> SendMessage(SystemMessageRequestModel model)
        {
            var result = new HttpResponseMessage(HttpStatusCode.OK);
            return Task.FromResult(result);
        }
    }
}