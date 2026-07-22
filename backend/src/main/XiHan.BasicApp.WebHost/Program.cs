// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Serilog;
using XiHan.BasicApp.WebHost;
using XiHan.Framework.Core.Extensions.DependencyInjection;
using XiHan.Framework.Utils.Logging;
using XiHan.Framework.Web.Core.Extensions.DependencyInjection;

try
{
    var builder = WebApplication.CreateBuilder(args);
    var configuredUrls = builder.Configuration["Hosting:Urls"];
    if (!string.IsNullOrWhiteSpace(configuredUrls))
    {
        builder.WebHost.UseUrls(configuredUrls);
    }

    await builder.AddApplicationAsync<XiHanBasicAppWebHostModule>();

    var app = builder.Build();

    await app.InitializeApplicationAsync();

    await app.RunAsync();

    Log.Information("应用启动");
}
catch (Exception ex)
{
    LogHelper.Error(ex, "应用异常");
    Log.Fatal(ex, "应用关闭");
}
finally
{
    await Log.CloseAndFlushAsync();
}
