// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

using Microsoft.AspNetCore.Mvc.Filters;

namespace MarginTrading.AccountsManagement.Filters
{
    /// <summary>
    /// Saves the parameter of type <typeparamref name="T"/>
    /// to the HttpContext.Items with the specified key
    /// </summary>
    /// <typeparam name="T">
    /// The type of the parameter. If there are multiple parameters of this type,
    /// the first one will be used
    /// </typeparam>
    /// <exception cref="ArgumentNullException">
    /// if key is null or whitespace
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// if failed to add the parameter to the HttpContext.Items
    /// </exception>
    internal sealed class PersistModelToContextFilter<T> : IActionFilter
    {
        private readonly string _key;

        public PersistModelToContextFilter(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            _key = key;
        }

        public void OnActionExecuting(ActionExecutingContext context)
        {
            var model = ExtractFirstOfType<T>(context.ActionArguments.Values);
            
            if (model == null)
                return;
            
            if (!TryPersist(context.HttpContext.Items, _key, model))
                throw new InvalidOperationException($"Failed to add {nameof(model)} to HttpContext with key {_key}");
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
        }

        public static TValue ExtractFirstOfType<TValue>(ICollection<object> source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            var results = source.OfType<TValue>();
            
            return results.FirstOrDefault();
        }

        public static bool TryPersist<TValue>(IDictionary<object, object> store, string key, TValue model)
        {
            if (store == null)
                throw new ArgumentNullException(nameof(store));

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            return store.TryAdd(key, model);
        }
    }
}