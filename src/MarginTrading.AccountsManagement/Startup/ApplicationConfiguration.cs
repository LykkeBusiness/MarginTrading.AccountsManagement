// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Lykke.Common.Api.Contract.Responses;
using Lykke.Common.ApiLibrary.Middleware;
using Lykke.Snow.Common.Correlation;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;

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
            app.UseLykkeMiddleware(
                Microsoft.Extensions.PlatformAbstractions.PlatformServices.Default.Application.ApplicationName, 
                ex => ex.ToString(), false);
#else
                app.UseLykkeMiddleware("Account Management Service", ex => new ErrorResponse {ErrorMessage = ex.Message}, false);
#endif

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();
            app.ConfigureSwagger();
            app.MapControllers();
            app.RegisterHooks();
            app.UseMiddleware<GlobalErrorHandlerMiddleware>();
            
            return app;
        }
    }
}