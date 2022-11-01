// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using MarginTrading.AccountsManagement.InternalModels;

using Newtonsoft.Json;

namespace MarginTrading.AccountsManagement.Repositories.Implementation
{
    internal static class DeserializeUtils
    {
        [Pure]
        public static decimal? DeserializeTemporaryCapital(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            List<TemporaryCapital> deseralized;
            try
            {
                deseralized = JsonConvert.DeserializeObject<List<TemporaryCapital>>(json);
            }
            catch (JsonReaderException)
            {
                return null;
            }
            catch (JsonSerializationException)
            {
                return null;
            }
            
            var result = deseralized?.Sum(x => x.Amount);

            return result;
        }
    }
}