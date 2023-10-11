// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Newtonsoft.Json;

namespace MarginTrading.AccountsManagement.Settings
{
    public class OidcSettings
    {
        [JsonProperty(PropertyName = "Api-Authority")]
        public string ApiAuthority { get; set; }
        
        [JsonProperty(PropertyName = "Client-Id")]
        public string ClientId { get; set; }
        
        [JsonProperty(PropertyName = "Client-Secret")]
        public string ClientSecret { get; set; }
        
        [JsonProperty(PropertyName = "Client-Scope")]
        public string ClientScope { get; set; }
        
        [JsonProperty(PropertyName = "Validate-Issuer-Name")]
        public bool ValidateIssuerName { get; set; }
        
        [JsonProperty(PropertyName = "Require-Https")]
        public bool RequireHttps { get; set; }
        
        [JsonProperty(PropertyName = "Renew-Token-Timeout-Sec")]
        public int RenewTokenTimeoutSec { get; set; }
    }
}