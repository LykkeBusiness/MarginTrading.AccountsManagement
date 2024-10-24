// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Lykke.Contracts.Responses;

using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Infrastructure;
using MarginTrading.Backend.Contracts;
using MarginTrading.Backend.Contracts.Account;

using Microsoft.AspNetCore.Http;

namespace MarginTrading.AccountsManagement.Services.Implementation
{
    /// <summary>
    /// Decorator for <see cref="IAccountsApi"/> that provides data
    /// from HttpContext items if they are present 
    /// </summary>
    public sealed class AccountsApiDecorator : IAccountsApi
    {
        private readonly IAccountsApi _decoratee;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IConvertService _convertService;

        public const string HttpContextDisposableCapitalKey = "disposable-capital-figures";

        public AccountsApiDecorator(
            IAccountsApi decoratee,
            IHttpContextAccessor httpContextAccessor,
            IConvertService convertService)
        {
            _decoratee = decoratee ?? throw new ArgumentNullException(nameof(decoratee));
            _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
            _convertService = convertService ?? throw new ArgumentNullException(nameof(convertService));
        }

        public Task<List<AccountStatContract>> GetAllAccountStats()
        {
            return _decoratee.GetAllAccountStats();
        }

        public Task<PaginatedResponse<AccountStatContract>> GetAllAccountStatsByPages(int? skip = null, int? take = null)
        {
            return _decoratee.GetAllAccountStatsByPages(skip, take);
        }

        public Task<List<string>> GetAllAccountIdsFiltered(ActiveAccountsRequest request)
        {
            return _decoratee.GetAllAccountIdsFiltered(request);
        }

        public Task<AccountStatContract> GetAccountStats(string accountId)
        {
            return _decoratee.GetAccountStats(accountId);
        }

        /// <summary>
        /// Take capital figures from HttpContext.Items first if they are present.
        /// </summary>
        /// <param name="accountId"></param>
        /// <returns></returns>
        public Task<AccountCapitalFigures> GetCapitalFigures(string accountId)
        {
            if (_httpContextAccessor.HttpContext == null)
                return _decoratee.GetCapitalFigures(accountId);

            var getDisposableCapitalRequest = ExtractOfType<GetDisposableCapitalRequest>(
                _httpContextAccessor.HttpContext.Items,
                HttpContextDisposableCapitalKey);

            if (getDisposableCapitalRequest?.CapitalFigures == null)
                return _decoratee.GetCapitalFigures(accountId);

            var accountCapitalFigures = _convertService.Convert<AccountCapitalFigures>(
                getDisposableCapitalRequest.CapitalFigures);
            return Task.FromResult(accountCapitalFigures);
        }

        public Task ResumeLiquidation(string accountId, string comment)
        {
            return _decoratee.ResumeLiquidation(accountId, comment);
        }
        
        /// <summary>
        /// Extracts value from dictionary by key and tries to cast it to a type <typeparamref name="T"/>
        /// </summary>
        /// <param name="source"></param>
        /// <param name="key"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="InvalidCastException">
        /// if value cannot be cast to a type <typeparamref name="T"/>
        /// </exception>
        public static T ExtractOfType<T>(IDictionary<object, object> source, string key)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (source.Count == 0)
                return default;
            
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentNullException(nameof(key));

            if (!source.TryGetValue(key, out var value))
                return default;

            if (value is T result)
                return result;

            throw new InvalidCastException($"Failed to cast value to a type {typeof(T).Name}");
        }
    }
}