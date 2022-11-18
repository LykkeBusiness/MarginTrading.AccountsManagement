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
            app.UseLykkeMiddleware(ex => ex.ToString());
#else
            app.UseLykkeMiddleware(ex => new ErrorResponse {ErrorMessage = ex.Message});
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