// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using System;

using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Snow.Common.AssemblyLogging;
using Lykke.Snow.Common.Correlation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MarginTrading.AccountsManagement.Startup
{
    internal static class ApplicationConfiguration
    {
        public static WebApplication Configure(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseHsts();
            }

            app.UseCorrelation();
#if DEBUG
            app.UseLykkeMiddleware(Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationName, 
                ex => ex.ToString(), false, false);
#else
            app.UseLykkeMiddleware("Account Management Service", ex => new ErrorResponse {ErrorMessage = ex.Message}, false, false);
#endif

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.ConfigureSwagger();
            app.MapControllers();
            app.RegisterHooks();
            
            return app;
        }
    }
}