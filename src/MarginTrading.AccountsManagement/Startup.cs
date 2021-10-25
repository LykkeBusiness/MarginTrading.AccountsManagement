// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Autofac;
using Autofac.Extensions.DependencyInjection;
using AzureStorage.Tables;
using Common.Log;
using JetBrains.Annotations;
using Lykke.AzureStorage.Tables.Entity.Metamodel;
using Lykke.AzureStorage.Tables.Entity.Metamodel.Providers;
using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Common.ApiLibrary.Swagger;
using Lykke.Logs;
using Lykke.Logs.MsSql;
using Lykke.Logs.MsSql.Repositories;
using Lykke.Logs.Serilog;
using Lykke.SettingsReader;
using Lykke.Snow.Common.Correlation;
using Lykke.Snow.Common.Correlation.Serilog;
using Lykke.Snow.Common.Startup;
using Lykke.Snow.Common.Startup.ApiKey;
using Lykke.Snow.Common.Startup.Hosting;
using Lykke.Snow.Common.Startup.Log;
using Lykke.Snow.Mdm.Contracts.BrokerFeatures;
using MarginTrading.AccountsManagement.Extensions;
using MarginTrading.AccountsManagement.Infrastructure.Implementation;
using MarginTrading.AccountsManagement.InternalModels;
using MarginTrading.AccountsManagement.Modules;
using MarginTrading.AccountsManagement.Services;
using MarginTrading.AccountsManagement.Services.Implementation;
using MarginTrading.AccountsManagement.Settings;
using MarginTrading.AccountsManagement.Workflow.ProductComplexity;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Serilog.Core;
using IApplicationLifetime = Microsoft.AspNetCore.Hosting.IApplicationLifetime;

namespace MarginTrading.AccountsManagement
{
    public class Startup
    {
        private IReloadingManager<AppSettings> _mtSettingsManager;
        public static string ServiceName { get; } = PlatformServices.Default.Application.ApplicationName;

        private IHostEnvironment Environment { get; }
        private ILifetimeScope ApplicationContainer { get; set; }
        private IConfigurationRoot Configuration { get; }
        [CanBeNull] private ILog Log { get; set; }
        
        public Startup(IHostEnvironment env)
        {
            Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddSerilogJson(env)
                .AddEnvironmentVariables()
                .Build();
            Environment = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            try
            {
                services
                    .AddApplicationInsightsTelemetry()
                    .AddControllers()
                    .AddNewtonsoftJson(options =>
                    {
                        options.SerializerSettings.ContractResolver = new DefaultContractResolver();
                        options.SerializerSettings.Converters.Add(new StringEnumConverter());
                    });

                _mtSettingsManager = Configuration.LoadSettings<AppSettings>(
                    throwExceptionOnCheckError: !Configuration.NotThrowExceptionsOnServiceValidation());

                services.AddApiKeyAuth(_mtSettingsManager.CurrentValue.MarginTradingAccountManagementServiceClient);

                services.AddSwaggerGen(options =>
                {
                    options.DefaultLykkeConfiguration("v1", ServiceName + " API");
                    var contractsXmlPath = Path.Combine(PlatformServices.Default.Application.ApplicationBasePath,
                        "MarginTrading.AccountsManagement.Contracts.xml");
                    options.IncludeXmlComments(contractsXmlPath);
                    options.OperationFilter<CustomOperationIdOperationFilter>();
                    if (!string.IsNullOrWhiteSpace(_mtSettingsManager.CurrentValue
                        .MarginTradingAccountManagementServiceClient?.ApiKey))
                    {
                        options.AddApiKeyAwareness();
                    }
                }).AddSwaggerGenNewtonsoftSupport();

                services.AddStackExchangeRedisCache(o =>
                {
                    o.Configuration = _mtSettingsManager.CurrentValue.MarginTradingAccountManagement.Cache.RedisConfiguration;
                    o.InstanceName = "AccountManagement:";
                });

                services.AddSingleton<AccountsCache>();

                services.AddCorrelation();
                
                Log = CreateLog(Configuration, _mtSettingsManager, services);

                services.AddSingleton<ILoggerFactory>(x => new WebHostLoggerFactory(Log));
                
                services.AddFeatureManagement(_mtSettingsManager.CurrentValue.MarginTradingAccountManagement.BrokerId);
                services.AddProductComplexity(_mtSettingsManager.CurrentValue);
            }
            catch (Exception ex)
            {
                Log?.WriteFatalErrorAsync(nameof(Startup), nameof(ConfigureServices), "", ex).Wait();
                throw;
            }
        }
        
        [UsedImplicitly]
        public void ConfigureContainer(ContainerBuilder builder)
        {
            builder.RegisterModule(new AccountsManagementModule(_mtSettingsManager, Log));
            builder.RegisterModule(new AccountsManagementExternalServicesModule(_mtSettingsManager));
            builder.RegisterModule(new CqrsModule(
                _mtSettingsManager.CurrentValue.MarginTradingAccountManagement.Cqrs,
                Log));
        }

        [UsedImplicitly]
        public void Configure(IApplicationBuilder app, IHostEnvironment env, IApplicationLifetime appLifetime)
        {
            try
            {
                ApplicationContainer = app.ApplicationServices.GetAutofacRoot();
                
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                else
                {
                    app.UseHsts();
                }

                app.UseCorrelation();
#if DEBUG
                app.UseLykkeMiddleware(ServiceName, ex => ex.ToString());
#else
                app.UseLykkeMiddleware(ServiceName, ex => new ErrorResponse {ErrorMessage = ex.Message});
#endif

                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                });
                app.UseSwagger();
                app.UseSwaggerUI(a => a.SwaggerEndpoint("/swagger/v1/swagger.json", "Main Swagger"));

                appLifetime.ApplicationStarted.Register(() => StartApplication().Wait());
                appLifetime.ApplicationStopping.Register(() => StopApplication().Wait());
                appLifetime.ApplicationStopped.Register(() => CleanUp().Wait());
                
                var provider = new AnnotationsBasedMetamodelProvider();

                EntityMetamodel.Configure(provider);
            }
            catch (Exception ex)
            {
                Log?.WriteFatalErrorAsync(nameof(Startup), nameof(ConfigureServices), "", ex).Wait();
                throw;
            }
        }


        private async Task StartApplication()
        {
            try
            {
                // NOTE: Service not yet receives and processes requests here
                Program.AppHost.WriteLogs(Environment, LogLocator.Log);

                await ApplicationContainer.Resolve<IStartupManager>().StartAsync();

                await Log.WriteMonitorAsync("", "", "Started");
            }
            catch (Exception ex)
            {
                await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StartApplication), "", ex);
                throw;
            }
        }

        private async Task StopApplication()
        {
            try
            {
                // NOTE: Service still can receive and process requests here, so take care about it if you add logic here.
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(StopApplication), "", ex);
                }

                throw;
            }
        }

        private async Task CleanUp()
        {
            try
            {
                // NOTE: Service can't receive and process requests here, so you can destroy all resources

                if (Log != null)
                {
                    await Log.WriteMonitorAsync("", "", "Terminating");
                }

                ApplicationContainer.Dispose();
            }
            catch (Exception ex)
            {
                if (Log != null)
                {
                    await Log.WriteFatalErrorAsync(nameof(Startup), nameof(CleanUp), "", ex);
                    (Log as IDisposable)?.Dispose();
                }

                throw;
            }
        }

        private static ILog CreateLog(IConfiguration configuration, IReloadingManager<AppSettings> settings, IServiceCollection services)
        {
            var aggregateLogger = new AggregateLogger();
            var consoleLogger = new LogToConsole();
            
            aggregateLogger.AddLog(consoleLogger);

            if (settings.CurrentValue.MarginTradingAccountManagement.UseSerilog)
            {
                var correlationContextAccessor = services.BuildServiceProvider().GetService<CorrelationContextAccessor>();
                aggregateLogger.AddLog(new SerilogLogger(typeof(Startup).Assembly, configuration, new List<ILogEventEnricher>
                {
                    new CorrelationLogEventEnricher("CorrelationId", correlationContextAccessor)
                }));
            }
            else if (settings.CurrentValue.MarginTradingAccountManagement.Db.StorageMode == StorageMode.SqlServer.ToString())
            {
                var sqlLogger = new LogToSql(new SqlLogRepository("AccountManagementLog",
                    settings.CurrentValue.MarginTradingAccountManagement.Db.LogsConnString));

                aggregateLogger.AddLog(sqlLogger);
            } 
            else if (settings.CurrentValue.MarginTradingAccountManagement.Db.StorageMode == StorageMode.Azure.ToString())
            {
                var dbLogConnectionStringManager = settings.Nested(x => x.MarginTradingAccountManagement.Db.LogsConnString);
                var dbLogConnectionString = dbLogConnectionStringManager.CurrentValue;
    
                if (string.IsNullOrEmpty(dbLogConnectionString))
                {
                    consoleLogger.WriteWarningAsync(nameof(Startup), nameof(CreateLog), "Table logger is not initialized").Wait();
                    return aggregateLogger;
                }
    
                if (dbLogConnectionString.StartsWith("${") && dbLogConnectionString.EndsWith("}"))
                    throw new InvalidOperationException($"LogsConnString {dbLogConnectionString} is not filled in settings");
    
                var persistenceManager = new LykkeLogToAzureStoragePersistenceManager(
                    AzureTableStorage<Lykke.Logs.LogEntity>.Create(dbLogConnectionStringManager, "AccountManagementLog", consoleLogger),
                    consoleLogger);
    
                // Creating azure storage logger, which logs own messages to console log
                var azureStorageLogger = new LykkeLogToAzureStorage(persistenceManager, null, consoleLogger);
                
                azureStorageLogger.Start();
                
                aggregateLogger.AddLog(azureStorageLogger);
            }

            LogLocator.Log = aggregateLogger;

            return aggregateLogger;
        }
    }
}