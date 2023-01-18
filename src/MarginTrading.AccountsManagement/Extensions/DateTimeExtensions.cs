// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;

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

        public static DateTime MaxDateTime(DateTime first, DateTime second)
        {
            if(Comparer<DateTime>.Default.Compare(first, second) > 0)
                return first;

            return second;
        }

        /// <summary>
        /// Updates each element in given collection in a way that given property is set to greater datetime among two values. 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="getFirstTimeStamp">The delegate that returns the first DateTime property for comparison</param>
        /// <param name="getSecondTimestamp">The delegate that returns the second DateTime property for comparison</param>
        /// <param name="updateProperty">The action that updates desired property with most recent DateTime property.</param>
        public static void OverwriteTimestampWithMostRecent<T>(IEnumerable<T> collection, 
            Func<T, DateTime> getFirstTimeStamp,
            Func<T, DateTime> getSecondTimestamp,
            Action<T, DateTime> updateProperty)
        {
            foreach(var item in collection)
            {
                var datetime1 = getFirstTimeStamp(item);
                var datetime2 = getSecondTimestamp(item);

                var mostRecent = MaxDateTime(datetime1, datetime2);

                updateProperty.Invoke(item, mostRecent);
            }
        }
    }
}