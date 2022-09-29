// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using AutoMapper;

namespace MarginTrading.AccountsManagement.Infrastructure
{
    public interface IConvertService
    {
        TResult Convert<TSource, TResult>(TSource source, Action<IMappingOperationOptions<TSource, TResult>> opts);
        TResult Convert<TSource, TResult>(TSource source);
        TResult Convert<TResult>(object source);
        void AssertConfigurationIsValid();
    }
}