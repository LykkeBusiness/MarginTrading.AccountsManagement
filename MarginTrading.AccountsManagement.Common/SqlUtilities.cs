// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System.Linq;

namespace MarginTrading.AccountsManagement.Dal.Common
{
    public static class SqlUtilities
    {
        public static string GetColumns<TEntity>() =>
            string.Join(",", typeof(TEntity).GetProperties().Select(x => x.Name));

        public static string GetFields<TEntity>() =>
            string.Join(",", typeof(TEntity).GetProperties().Select(x => "@" + x.Name));
    }
}