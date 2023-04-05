// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using Autofac;
using BookKeeper.Client.Workflow.Events;
using Lykke.Cqrs;
using Lykke.Cqrs.Configuration;
using Lykke.Cqrs.Configuration.BoundedContext;
using Lykke.Cqrs.Configuration.Routing;
using Lykke.Cqrs.Configuration.Saga;
using Lykke.Cqrs.Middleware.Logging;
using Lykke.Messaging.Serialization;
using Lykke.Snow.Common.Correlation.Cqrs;
using Lykke.Snow.Cqrs;
using MarginTrading.AccountsManagement.Contracts.Commands;
using MarginTrading.AccountsManagement.Contracts.Events;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Services.Implementation;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Workflow.ClosePosition;
using MarginTrading.AccountsManagement.Workflow.DeleteAccounts;
using MarginTrading.AccountsManagement.Workflow.DeleteAccounts.Commands;
using MarginTrading.AccountsManagement.Workflow.DeleteAccounts.Events;
using MarginTrading.AccountsManagement.Workflow.Deposit;
using MarginTrading.AccountsManagement.Workflow.Deposit.Commands;
using MarginTrading.AccountsManagement.Workflow.Deposit.Events;
using MarginTrading.AccountsManagement.Workflow.GiveTemporaryCapital;
using MarginTrading.AccountsManagement.Workflow.GiveTemporaryCapital.Commands;
using MarginTrading.AccountsManagement.Workflow.GiveTemporaryCapital.Events;
using MarginTrading.AccountsManagement.Workflow.NegativeProtection;
using MarginTrading.AccountsManagement.Workflow.NegativeProtection.Commands;
using MarginTrading.AccountsManagement.Workflow.Projections;
using MarginTrading.AccountsManagement.Workflow.RevokeTemporaryCapital;
using MarginTrading.AccountsManagement.Workflow.RevokeTemporaryCapital.Commands;
using MarginTrading.AccountsManagement.Workflow.RevokeTemporaryCapital.Events;
using MarginTrading.AccountsManagement.Workflow.UpdateBalance;
using MarginTrading.AccountsManagement.Workflow.UpdateBalance.Commands;
using MarginTrading.AccountsManagement.Workflow.Withdrawal;
using MarginTrading.AccountsManagement.Workflow.Withdrawal.Commands;
using MarginTrading.AccountsManagement.Workflow.Withdrawal.Events;
using MarginTrading.Backend.Contracts.TradingSchedule;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace MarginTrading.AccountsManagement.Modules
{
    internal class CqrsModule : Module
    {
        private const string DefaultRoute = "self";
        private const string DefaultPipeline = "commands";
        private readonly CqrsSettings _settings;
        private readonly long _defaultRetryDelayMs;
        private readonly CqrsContextNamesSettings _contextNames;

        public CqrsModule(CqrsSettings settings)
        {
            _settings = settings;
            _defaultRetryDelayMs = (long) _settings.RetryDelay.TotalMilliseconds;
            _contextNames = _settings.ContextNames;
        }

        protected override void Load(ContainerBuilder builder)
        {
            builder.Register(context => new AutofacDependencyResolver(context)).As<IDependencyResolver>()
                .SingleInstance();
            builder.RegisterType<CqrsSender>().As<ICqrsSender>()
                .PropertiesAutowired(PropertyWiringOptions.AllowCircularDependencies)
                .SingleInstance();

            // Sagas & command handlers
            builder.RegisterAssemblyTypes(GetType().Assembly)
                .Where(t => t.Name.EndsWith("Saga") || t.Name.EndsWith("CommandsHandler") || t.Name.EndsWith("Projection"))
                .AsSelf();

            builder.Register(CreateEngine)
                .As<ICqrsEngine>()
                .SingleInstance();
        }

        private CqrsEngine CreateEngine(IComponentContext ctx)
        {
            var rabbitMqConventionEndpointResolver = new RabbitMqConventionEndpointResolver(
                "RabbitMq",
                SerializationFormat.MessagePack,
                environment: _settings.EnvironmentName);

            var rabbitMqSettings = new ConnectionFactory
            {
                Uri = new Uri(_settings.ConnectionString, UriKind.Absolute)
            };
            
            var loggerFactory = ctx.Resolve<ILoggerFactory>();
            
            var engine = new RabbitMqCqrsEngine(
                loggerFactory,
                ctx.Resolve<IDependencyResolver>(),
                new DefaultEndpointProvider(),
                rabbitMqSettings.Endpoint.ToString(),
                rabbitMqSettings.UserName,
                rabbitMqSettings.Password,
                true,
                Register.DefaultEndpointResolver(rabbitMqConventionEndpointResolver),
                RegisterDefaultRouting(),
                RegisterWithdrawalSaga(),
                RegisterDepositSaga(),
                RegisterClosePositionSaga(),
                RegisterNegativeProtectionSaga(),
                RegisterGiveTemporaryCapitalSaga(),
                RegisterRevokeTemporaryCapitalSaga(),
                RegisterDeleteAccountsSaga(),
                RegisterContext(),
                Register.CommandInterceptors(new DefaultCommandLoggingInterceptor(loggerFactory)),
                Register.EventInterceptors(new DefaultEventLoggingInterceptor(loggerFactory)));
            var correlationManager = ctx.Resolve<CqrsCorrelationManager>();
            engine.SetReadHeadersAction(correlationManager.FetchCorrelationIfExists);
            engine.SetWriteHeadersFunc(correlationManager.BuildCorrelationHeadersIfExists);

            return engine;
        }

        private IRegistration RegisterContext()
        {
            var contextRegistration = Register.BoundedContext(_contextNames.AccountsManagement)
                .FailedCommandRetryDelay(_defaultRetryDelayMs)
                .ProcessingOptions(DefaultRoute).MultiThreaded(8).QueueCapacity(1024);
            RegisterWithdrawalCommandHandler(contextRegistration);
            RegisterDepositCommandHandler(contextRegistration);
            RegisterUpdateBalanceCommandHandler(contextRegistration);
            RegisterNegativeProtectionCommandsHandler(contextRegistration);
            RegisterGiveTemporaryCapitalCommandsHandler(contextRegistration);
            RegisterRevokeTemporaryCapitalCommandsHandler(contextRegistration);
            RegisterDeleteAccountsCommandsHandler(contextRegistration);
            RegisterProjections(contextRegistration);
            
            return contextRegistration;
        }

        private PublishingCommandsDescriptor<IDefaultRoutingRegistration> RegisterDefaultRouting()
        {
            return Register.DefaultRouting
                .PublishingCommands(
                    typeof(UpdateBalanceInternalCommand),
                    typeof(WithdrawCommand),
                    typeof(DepositCommand),
                    typeof(StartGiveTemporaryCapitalInternalCommand),
                    typeof(StartRevokeTemporaryCapitalInternalCommand),
                    typeof(DeleteAccountsCommand)
                )
                .To(_contextNames.AccountsManagement)
                .With(DefaultPipeline);
        }

        private IRegistration RegisterWithdrawalSaga()
        {
            var sagaRegistration = RegisterSaga<WithdrawalSaga>();

            sagaRegistration
                .ListeningEvents(
                    typeof(WithdrawalStartedInternalEvent),
                    typeof(AccountBalanceChangeFailedEvent))
                .From(_contextNames.AccountsManagement)
                .On(DefaultRoute)
                .PublishingCommands(
                    typeof(FreezeAmountForWithdrawalCommand),
                    typeof(UnfreezeMarginOnFailWithdrawalCommand))
                .To(_contextNames.TradingEngine)
                .With(DefaultPipeline);

            sagaRegistration
                .ListeningEvents(
                    typeof(AmountForWithdrawalFrozenEvent), 
                    typeof(AmountForWithdrawalFreezeFailedEvent),
                    typeof(UnfreezeMarginOnFailSucceededWithdrawalEvent))
                .From(_contextNames.TradingEngine)
                .On(DefaultRoute)
                .PublishingCommands(
                    typeof(UpdateBalanceInternalCommand),
                    typeof(FailWithdrawalInternalCommand))
                .To(_contextNames.AccountsManagement)
                .With(DefaultPipeline);
            
            sagaRegistration
                .ListeningEvents(
                    typeof(WithdrawalFailedEvent),
                    typeof(WithdrawalSucceededEvent))
                .From(_contextNames.AccountsManagement)
                .On(DefaultRoute);
            
            sagaRegistration
                .ListeningEvents(
                    typeof(AccountChangedEvent),
                    typeof(WithdrawalStartFailedInternalEvent))
                .From(_contextNames.AccountsManagement)
                .On(DefaultRoute)
                .PublishingCommands(
                    typeof(CompleteWithdrawalInternalCommand),
                    typeof(FailWithdrawalInternalCommand))
                .To(_contextNames.AccountsManagement)
                .With(DefaultPipeline);

            return sagaRegistration;
        }

        private static void RegisterWithdrawalCommandHandler(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningCommands(
                    typeof(WithdrawCommand),
                    typeof(FailWithdrawalInternalCommand),
                    typeof(CompleteWithdrawalInternalCommand))
                .On(DefaultRoute)
                .WithCommandsHandler<WithdrawalCommandsHandler>()
                .PublishingEvents(
                    typeof(WithdrawalFailedEvent),
                    typeof(WithdrawalStartedInternalEvent),
                    typeof(WithdrawalStartFailedInternalEvent),
                    typeof(WithdrawalSucceededEvent))
                .With(DefaultPipeline);
        }

        private IRegistration RegisterDepositSaga()
        {
            return RegisterSaga<DepositSaga>()
                .ListeningEvents(
                    typeof(DepositStartedInternalEvent),
                    typeof(AccountChangedEvent), 
                    typeof(AccountBalanceChangeFailedEvent),
                    typeof(AmountForDepositFrozenInternalEvent),
                    typeof(AmountForDepositFreezeFailedInternalEvent))
                .From(_contextNames.AccountsManagement)
                .On(DefaultRoute)
                .PublishingCommands(
                    typeof(FreezeAmountForDepositInternalCommand),
                    typeof(UpdateBalanceInternalCommand),
                    typeof(FailDepositInternalCommand),
                    typeof(CompleteDepositInternalCommand))
                .To(_contextNames.AccountsManagement)
                .With(DefaultPipeline);
        }

        private static void RegisterDepositCommandHandler(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration
                .ListeningCommands(
                    typeof(DepositCommand),
                    typeof(FreezeAmountForDepositInternalCommand),
                    typeof(FailDepositInternalCommand),
                    typeof(CompleteDepositInternalCommand))
                .On(DefaultRoute)
                .WithCommandsHandler<DepositCommandsHandler>()
                .PublishingEvents(
                    typeof(DepositFailedEvent),
                    typeof(AmountForDepositFrozenInternalEvent),
                    typeof(DepositStartedInternalEvent),
                    typeof(DepositSucceededEvent))
                .With(DefaultPipeline);
        }

        private static void RegisterUpdateBalanceCommandHandler(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration
                .ListeningCommands(
                    typeof(UpdateBalanceInternalCommand),
                    typeof(ChangeBalanceCommand))
                .On(DefaultRoute)
                .ProcessingOptions(DefaultRoute).MultiThreaded(10).QueueCapacity(1024)
                .WithCommandsHandler<UpdateBalanceCommandsHandler>()
                .PublishingEvents(
                    typeof(AccountChangedEvent),
                    typeof(AccountBalanceChangeFailedEvent))
                .With(DefaultPipeline)
                .ProcessingOptions(DefaultPipeline).MultiThreaded(10).QueueCapacity(1024);
        }

        private IRegistration RegisterClosePositionSaga()
        {
            return RegisterSaga<ClosePositionSaga>()
                .ListeningEvents(
                    typeof(Backend.Contracts.Events.PositionClosedEvent))
                .From(_contextNames.TradingEngine)
                .On(DefaultRoute)
                .PublishingCommands(
                    typeof(UpdateBalanceInternalCommand))
                .To(_contextNames.AccountsManagement)
                .With(DefaultPipeline);
        }

        private IRegistration RegisterNegativeProtectionSaga()
        {
            return RegisterSaga<NegativeProtectionSaga>()
                .ListeningEvents(typeof(AccountChangedEvent))
                .From(_contextNames.AccountsManagement)
                .On(DefaultRoute)
                .PublishingCommands(
                    typeof(NotifyNegativeProtectionInternalCommand))
                .To(_contextNames.AccountsManagement)
                .With(DefaultPipeline);
        }

        private static void RegisterNegativeProtectionCommandsHandler(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningCommands(
                    typeof(NotifyNegativeProtectionInternalCommand))
                .On(DefaultRoute)
                .WithCommandsHandler<NegativeProtectionCommandsHandler>()
                .PublishingEvents(
                    typeof(NegativeProtectionEvent))
                .With(DefaultPipeline);
        }

        private IRegistration RegisterGiveTemporaryCapitalSaga()
        {
            return RegisterSaga<GiveTemporaryCapitalSaga>()
                .ListeningEvents(
                    typeof(GiveTemporaryCapitalStartedInternalEvent),
                    typeof(GiveTemporaryCapitalSucceededEvent),
                    typeof(GiveTemporaryCapitalFailedEvent),
                    
                    typeof(AccountChangedEvent),
                    typeof(AccountBalanceChangeFailedEvent)
                )
                .From(_contextNames.AccountsManagement)
                .On(DefaultRoute)
                .PublishingCommands(
                    typeof(UpdateBalanceInternalCommand),
                    typeof(FinishGiveTemporaryCapitalInternalCommand)
                )
                .To(_contextNames.AccountsManagement)
                .With(DefaultPipeline);
        }

        private static void RegisterGiveTemporaryCapitalCommandsHandler(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningCommands(
                    typeof(StartGiveTemporaryCapitalInternalCommand),
                    typeof(FinishGiveTemporaryCapitalInternalCommand)
                )
                .On(DefaultRoute)
                .WithCommandsHandler<GiveTemporaryCapitalCommandsHandler>()
                .PublishingEvents(
                    typeof(GiveTemporaryCapitalStartedInternalEvent),
                    typeof(GiveTemporaryCapitalSucceededEvent),
                    typeof(GiveTemporaryCapitalFailedEvent)
                )
                .With(DefaultPipeline);
        }

        private IRegistration RegisterRevokeTemporaryCapitalSaga()
        {
            return RegisterSaga<RevokeTemporaryCapitalSaga>()
                .ListeningEvents(
                    typeof(RevokeTemporaryCapitalStartedInternalEvent),
                    typeof(RevokeTemporaryCapitalSucceededEvent),
                    typeof(RevokeTemporaryCapitalFailedEvent),
                    
                    typeof(AccountChangedEvent),
                    typeof(AccountBalanceChangeFailedEvent)
                )
                .From(_contextNames.AccountsManagement)
                .On(DefaultRoute)
                .PublishingCommands(
                    typeof(UpdateBalanceInternalCommand),
                    typeof(FinishRevokeTemporaryCapitalInternalCommand)
                )
                .To(_contextNames.AccountsManagement)
                .With(DefaultPipeline);
        }

        private static void RegisterRevokeTemporaryCapitalCommandsHandler(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningCommands(
                    typeof(StartRevokeTemporaryCapitalInternalCommand),
                    typeof(FinishRevokeTemporaryCapitalInternalCommand)
                )
                .On(DefaultRoute)
                .WithCommandsHandler<RevokeTemporaryCapitalCommandsHandler>()
                .PublishingEvents(
                    typeof(RevokeTemporaryCapitalStartedInternalEvent),
                    typeof(RevokeTemporaryCapitalSucceededEvent),
                    typeof(RevokeTemporaryCapitalFailedEvent)
                )
                .With(DefaultPipeline);
        }

        private IRegistration RegisterDeleteAccountsSaga()
        {
            var sagaRegistration = RegisterSaga<DeleteAccountsSaga>();
                
            sagaRegistration
                .ListeningEvents(
                    typeof(DeleteAccountsStartedInternalEvent),
                    typeof(AccountsMarkedAsDeletedEvent)
                )
                .From(_contextNames.AccountsManagement)
                .On(DefaultRoute)
                .PublishingCommands(
                    typeof(BlockAccountsForDeletionCommand),
                    typeof(MtCoreFinishAccountsDeletionCommand)
                )
                .To(_contextNames.TradingEngine)
                .With(DefaultPipeline);

            sagaRegistration
                .ListeningEvents(
                    typeof(AccountsBlockedForDeletionEvent),
                    typeof(MtCoreDeleteAccountsFinishedEvent)
                )
                .From(_contextNames.TradingEngine)
                .On(DefaultRoute)
                .PublishingCommands(
                    typeof(MarkAccountsAsDeletedInternalCommand),
                    typeof(FinishAccountsDeletionInternalCommand)
                )
                .To(_contextNames.AccountsManagement)
                .With(DefaultPipeline);
            
            sagaRegistration
                .ListeningEvents(
                    typeof(AccountsDeletionFinishedEvent)
                )
                .From(_contextNames.AccountsManagement)
                .On(DefaultRoute);

            return sagaRegistration;
        }

        private static void RegisterDeleteAccountsCommandsHandler(
            ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningCommands(
                    typeof(DeleteAccountsCommand),
                    typeof(MarkAccountsAsDeletedInternalCommand),
                    typeof(FinishAccountsDeletionInternalCommand)
                )
                .On(DefaultRoute)
                .WithCommandsHandler<DeleteAccountsCommandsHandler>()
                .PublishingEvents(
                    typeof(DeleteAccountsStartedInternalEvent),
                    typeof(AccountsMarkedAsDeletedEvent),
                    typeof(AccountsDeletionFinishedEvent),
                    typeof(AccountChangedEvent)
                )
                .With(DefaultPipeline);
        }
        
        private void RegisterProjections(ProcessingOptionsDescriptor<IBoundedContextRegistration> contextRegistration)
        {
            contextRegistration.ListeningEvents(
                    typeof(AccountChangedEvent))
                .From(_settings.ContextNames.AccountsManagement).On("events")
                .WithProjection(
                    typeof(AccountChangedProjection), _settings.ContextNames.AccountsManagement);
            
            contextRegistration.ListeningEvents(
                    typeof(MarketStateChangedEvent))
                .From(_settings.ContextNames.TradingEngine).On("events")
                .WithProjection(
                    typeof(MarketStateChangedProjection), _settings.ContextNames.TradingEngine);

            contextRegistration.ListeningEvents(
                    typeof(TaxFileUploadedEvent))
                .From(_settings.ContextNames.BookKeeper).On("events")
                .WithProjection(
                    typeof(TaxFileUploadedProjection), _settings.ContextNames.BookKeeper);
        }

        private ISagaRegistration RegisterSaga<TSaga>()
        {
            return Register.Saga<TSaga>($"{_contextNames.AccountsManagement}.{typeof(TSaga).Name}");
        }
    }
}