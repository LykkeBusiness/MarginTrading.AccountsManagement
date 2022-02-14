// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

namespace MarginTrading.AccountsManagement.Extensions
{
    public static class DateTimeExtensions
    {
        public static DateTime AssumeUtcIfUnspecified(this DateTime dateTime)
        {
            switch (dateTime.Kind)
            {
                case DateTimeKind.Unspecified:
                    return DateTime.SpecifyKind(dateTime, DateTimeKind.Utc);
                default:
                    return dateTime.ToUniversalTime();
            }
        }
    }
}