// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Net;
using System.Threading.Tasks;

using NUnit.Framework;

namespace MarginTrading.AccountsManagement.IntegrationTests
{
    public class AccountsControllerTests
    {
        [Test(Description = "LT-4242")]
        public async Task GetStat_Account_Does_Not_Exist_Returns_Empty_Response()
        {
            var client = await TestBootstrapper.CreateTestClient();

            var response = await client.GetAsync($"/api/accounts/stat/non-existing-account-id");
            
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }

        [Test(Description = "LT-4242")]
        public async Task GetDisposableCapital_Account_Does_Not_Exist_Returns_Empty_Response()
        {
            var client = await TestBootstrapper.CreateTestClient();

            var response = await client.GetAsync($"/api/accounts/non-existing-account-id/disposable-capital");

            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }
    }
}