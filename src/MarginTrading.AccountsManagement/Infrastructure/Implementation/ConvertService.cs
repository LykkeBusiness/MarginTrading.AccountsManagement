// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;
using AutoMapper;
using JetBrains.Annotations;
using MarginTrading.AccountsManagement.Contracts.Audit;
using MarginTrading.AccountsManagement.Contracts.Models;
using MarginTrading.AccountsManagement.Contracts.Models.AdditionalInfo;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.InternalModels.Interfaces;
using Newtonsoft.Json;

namespace MarginTrading.AccountsManagement.Infrastructure.Implementation
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
                cfg.CreateMap<string, AccountBalanceChangeReasonType>()
                    .ConvertUsing(Enum.Parse<AccountBalanceChangeReasonType>);
                cfg.CreateMap<List<string>, string>().ConvertUsing(JsonConvert.SerializeObject);
                cfg.CreateMap<string, List<string>>().ConvertUsing(JsonConvert.DeserializeObject<List<string>>);
                cfg.CreateMap<IAccount, AccountContract>()
                    .ForMember(p => p.AdditionalInfo,
                        s => s.ResolveUsing(x => x.AdditionalInfo.Serialize()));
                cfg.CreateMap<IClient, ClientTradingConditionsContract>()
                    .ForMember(x => x.ClientId, o => o.MapFrom(s=> s.Id));
                cfg.CreateMap<IClientWithAccounts, ClientTradingConditionsSearchResultContract>()
                    .ForMember(x => x.ClientId, o => o.MapFrom(s => s.Id))
                    .ForMember(x => x.AccountIdentities,
                        o => o.ResolveUsing((src, dest, destMember, resContext) =>
                            dest.AccountIdentities = src.AccountIdentityCommaSeparatedList.Split(',').ToList()));
                cfg.CreateMap<IAccountSuggested, AccountSuggestedContract>()
                    .ForMember(d => d.Name,
                        o => o.MapFrom(s => s.AccountName));

                //Audit
                cfg.CreateMap<AuditModel, AuditContract>();
                cfg.CreateMap<GetAuditLogsRequest, AuditLogsFilterDto>();
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
    }
}