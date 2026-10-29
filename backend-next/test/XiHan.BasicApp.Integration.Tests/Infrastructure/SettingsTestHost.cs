// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Contracts.MultiTenancy;
using XiHan.Framework.Kernel.Hosting;
using XiHan.Framework.Kernel.Pipeline;
using XiHan.Framework.Primitives;

namespace XiHan.BasicApp.Integration.Tests.Infrastructure;

/// <summary>
/// 设置包用例的起手。
/// </summary>
/// <remarks>
/// 跑全部种子、超管登录、开通企业版租户 <c>contoso</c> 并初始化其管理员；另给进程内调用的入口（平台或指定租户）。
/// </remarks>
public static class SettingsTestHost
{
    /// <summary>
    /// 起宿主并准备两侧的令牌。
    /// </summary>
    public static async Task<(BasicAppTestServer Server, string Platform, string Tenant)> StartAsync(string connectionString, string provider = "sqlite", Action<WebApplicationBuilder>? configure = null)
    {
        var server = await BasicAppTestServer.StartAsync(connectionString, provider, configure: configure);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var platform = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;
        var enterprise = await EditionsTests.EditionIdAsync(server, platform, "enterprise");
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", $$"""{"tenantId":"contoso","name":"Contoso","editionId":"{{enterprise}}"}""", platform));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = createdBody.GetProperty("id").GetString()!;
        var (initialized, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{id}:initialize-admin", """{"userName":"admin","password":"Admin@123456"}""", platform));
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        var tenant = (await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", "contoso")).GetProperty("accessToken").GetString()!;
        return (server, platform, tenant);
    }

    /// <summary>
    /// 进程内发起一次调用（完整内核管道）：<paramref name="tenantId"/> 为空即平台上下文。
    /// </summary>
    public static async Task<T> InvokeAsync<T>(BasicAppTestServer server, string? tenantId, Func<InvocationContext, ValueTask<T>> action)
    {
        T result = default!;
        if (tenantId is not null)
        {
            var ran = await server.Services.GetRequiredService<ITenantScopeRunner>().RunAsync(tenantId, async ctx =>
            {
                result = await action(ctx);
                return Result.Success();
            }, CancellationToken.None);
            Assert.True(ran.IsSuccess, ran.Error?.Code);
            return result;
        }

        await using var scope = server.Services.GetRequiredService<IServiceScopeFactory>().CreateAsyncScope();
        var context = App.CreateContext(scope.ServiceProvider, CancellationToken.None);
        var outcome = await server.Services.GetRequiredService<App>().InvokeAsync(context, async ctx =>
        {
            result = await action(ctx);
            ctx.SetOutcome(Result.Success());
        });
        Assert.True(outcome.IsSuccess, outcome.Error?.Code);
        return result;
    }
}
