﻿// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using JetBrains.Annotations;
using Lykke.Snow.Common.Extensions;

using MarginTrading.AccountsManagement.Contracts.Api;
using MarginTrading.AccountsManagement.Contracts.Audit;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using MarginTrading.AccountsManagement.Repositories.Implementation.SQL;
using MarginTrading.Backend.Contracts.Account;

using Newtonsoft.Json;

using AccountBalanceChangeReasonType = MarginTrading.AccountsManagement.InternalModels.AccountBalanceChangeReasonType;
using AccountStatContract = MarginTrading.AccountsManagement.Contracts.Models.AccountStatContract;

namespace MarginTrading.AccountsManagement.Infrastructure.Implementation
{
    [UsedImplicitly]
    public class ConvertService : IConvertService
    {
        private readonly IMapper _mapper = CreateMapper();

        private static IMapper CreateMapper()
        {
            return new MapperConfiguration(cfg =>
            {
                cfg.CreateMap<AccountBalanceChangeReasonType, string>().ConvertUsing(x => x.ToString());
                cfg.CreateMap<string, AccountBalanceChangeReasonType>()
                    .ConvertUsing(x => Enum.Parse<AccountBalanceChangeReasonType>(x));
                cfg.CreateMap<IAccountBalanceChange, AccountBalanceChangeEntity>()
                    .ForMember(x => x.Oid, opt => opt.Ignore());
                cfg.CreateMap<List<string>, string>().ConvertUsing(x => JsonConvert.SerializeObject(x));
                cfg.CreateMap<string, List<string>>().ConvertUsing(x => JsonConvert.DeserializeObject<List<string>>(x));
                cfg.CreateMap<IAccount, AccountContract>()
                    .ForMember(p => p.AdditionalInfo,
                        s => s.MapFrom(x => x.AdditionalInfo.Serialize()))
                    .ForMember(p => p.ClientModificationTimestamp,
                        s => s.MapFrom(x => x.ClientModificationTimestamp.AssumeUtcIfUnspecified()))
                    .ForMember(p => p.TemporaryCapital,
                        s => s.MapFrom(x => x.TemporaryCapital.Sum(x => x.Amount)));
                cfg.CreateMap<IClient, ClientTradingConditionsContract>()
                    .ForMember(x => x.ClientId, o => o.MapFrom(s=> s.Id));
                cfg.CreateMap<IClientWithAccounts, ClientTradingConditionsSearchResultContract>()
                    .ForMember(x => x.ClientId, o => o.MapFrom(s => s.Id))
                    .ForMember(x => x.AccountIdentities, o => o.MapFrom(x => x.DeserializeAccounts()));
                cfg.CreateMap<IClientWithAccounts, ClientTradingConditionsWithAccountsContract>()
                    .ForMember(x => x.ClientId, o => o.MapFrom(s => s.Id))
                    .ForMember(x => x.AccountIdentities, o => o.MapFrom(x => x.DeserializeAccounts()));
                cfg.CreateMap<IAccountSuggested, AccountSuggestedContract>()
                    .ForMember(d => d.Name,
                        o => o.MapFrom(s => s.AccountName));
                cfg.CreateMap<AccountStat, AccountStatContract>();
                cfg.CreateMap<ILossPercentage, LossPercentageEntity>()
                    .ForMember(x => x.Id, o => o.Ignore());

                //Audit
                cfg.CreateMap<AuditModel, AuditContract>();
                cfg.CreateMap<GetAuditLogsRequest, AuditLogsFilterDto>();

                cfg.CreateMap<GetDisposableCapitalRequest.AccountCapitalFigures, AccountCapitalFigures>();
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