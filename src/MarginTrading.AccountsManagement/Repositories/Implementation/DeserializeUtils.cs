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
        public static List<TemporaryCapital> DeserializeTemporaryCapital(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<TemporaryCapital>();
            }

            List<TemporaryCapital> deseralized;
            try
            {
                deseralized = JsonConvert.DeserializeObject<List<TemporaryCapital>>(json);
            }
            catch (JsonReaderException)
            {
                return new List<TemporaryCapital>();
            }
            catch (JsonSerializationException)
            {
                return new List<TemporaryCapital>();
            }

            return deseralized;
        }
        
        [Pure]
        public static decimal Summarize(this List<TemporaryCapital> temporaryCapital)
        {
            return temporaryCapital?.Sum(x => x.Amount) ?? 0;
        }
    }
}