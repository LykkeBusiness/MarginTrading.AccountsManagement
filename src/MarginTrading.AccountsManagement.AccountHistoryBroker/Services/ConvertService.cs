// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using AutoMapper;
using JetBrains.Annotations;

using MarginTrading.AccountsManagement.AccountHistoryBroker.Models;
using MarginTrading.AccountsManagement.AccountHistoryBroker.Repositories.SqlRepositories;

using AccountBalanceChangeReasonType =
    MarginTrading.AccountsManagement.AccountHistoryBroker.Models.AccountBalanceChangeReasonType;

namespace MarginTrading.AccountsManagement.AccountHistoryBroker.Services
{
    [UsedImplicitly]
    internal class ConvertService : IConvertService
    {
        private readonly IMapper _mapper = CreateMapper();

        private static IMapper CreateMapper()
        {
            return new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<AccountBalanceChangeReasonType, string>().ConvertUsing(x => x.ToString());
                cfg.CreateMap<IAccountHistory, AccountHistoryEntity>();
            }).CreateMapper();
        }

        public TResult Convert<TSource, TResult>(TSource source,
            Action<IMappingOperationOptions<TSource, TResult>> opts)
        {
            return _mapper.Map(source, opts);
        }

        public TResult Convert<TSource, TResult>(TSource source)
        {
            return _mapper.Map<TSource, TResult>(source);
        }

        public TResult Convert<TResult>(object source)
        {
            return _mapper.Map<TResult>(source);
        }

        public void AssertConfigurationIsValid()
        {
            _mapper.ConfigurationProvider.AssertConfigurationIsValid();
        }
    }
}