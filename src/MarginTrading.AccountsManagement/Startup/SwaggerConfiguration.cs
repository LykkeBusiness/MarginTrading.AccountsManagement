// Copyright (c) 2019 Lykke Corp.
// See the LICENSE file in the project root for more information.

using Microsoft.AspNetCore.Builder;

namespace MarginTrading.AccountsManagement.Startup
{
    public static class SwaggerConfiguration
    {
        public static IApplicationBuilder ConfigureSwagger(this WebApplication app)
        {
            app.UseSwagger();
            app.UseSwaggerUI(a => a.SwaggerEndpoint("/swagger/v1/swagger.json", "Nova 2 Accounts Management API"));

            return app;
        }
    }
}