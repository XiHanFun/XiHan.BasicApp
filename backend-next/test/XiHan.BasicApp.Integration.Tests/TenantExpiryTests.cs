// Copyright (c) 2021-Present XiHanFun and contributors.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using XiHan.BasicApp.Integration.Tests.Infrastructure;
using XiHan.BasicApp.Tenancy.Jobs;
using XiHan.Framework.Primitives;
using XiHan.Framework.Scheduling.Abstractions.Jobs;
using XiHan.Framework.Scheduling.Abstractions.Schedulers;

namespace XiHan.BasicApp.Integration.Tests;

/// <summary>
/// 验证租户到期。
/// </summary>
/// <remarks>
/// 到期时间一过登录与刷新即被拒（IDENTITY_DENIED_019，不等作业）；到期作业把生效的租户标为到期、目录停用，带租户声明的调用被拒；
/// 恢复要先延长到期时间（TENANCY_CONFLICT_015），延长后恢复即可再登录，作业不再动它。
/// </remarks>
public sealed class TenantExpiryTests : IDisposable
{
    private readonly SqliteDatabase _database = new();

    public void Dispose() => _database.Dispose();

    [Fact]
    public async Task Expiry_RefusesSignIn_JobMarksExpired_ResumeNeedsRenewal()
    {
        await using var server = await BasicAppTestServer.StartAsync(_database.ConnectionString);
        await server.SeedDefaultsAsync();
        await server.RunSeedersAsync();
        var admin = (await AuthFlowTests.LoginAsync(server, "superadmin", "SuperAdmin@123")).GetProperty("accessToken").GetString()!;

        async Task<JsonElement> TenantAsync(string id)
        {
            var (response, body) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Get, $"/api/tenants/{id}", token: admin));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            return body.GetProperty("tenant");
        }

        async Task SetExpiryAsync(string id, DateTimeOffset expiresAt)
        {
            var (response, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Put, $"/api/tenants/{id}", $$"""{"name":"Fabrikam","expiresAt":"{{expiresAt:O}}"}""", admin));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        async Task<(HttpResponseMessage Response, JsonElement Body)> LoginAsync()
            => await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:login", """{"userName":"admin","password":"Admin@123456","tenant":"fabrikam"}""", acceptLanguage: "zh-CN"));

        async Task<Result> RunJobAsync() => await SettingsTestHost.InvokeAsync(server, null, async ctx =>
        {
            var job = ctx.Services.GetRequiredService<TenantExpiryJob>();
            return await job.RunAsync(JobRun.Create(TenantExpiryJob.Name, DateTimeOffset.UtcNow, JobTrigger.Manual), ctx, ctx.CancellationToken);
        });

        // 开通时带上一天后的到期时间，初始化管理员后可以登录
        var (created, createdBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants", $$"""{"tenantId":"fabrikam","name":"Fabrikam","expiresAt":"{{DateTimeOffset.UtcNow.AddDays(1):O}}"}""", admin));
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var id = createdBody.GetProperty("id").GetString()!;
        var (initialized, _) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, $"/api/tenants/{id}:initialize-admin", """{"userName":"admin","password":"Admin@123456"}""", admin));
        Assert.Equal(HttpStatusCode.OK, initialized.StatusCode);
        var login = await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", "fabrikam");
        var tenantToken = login.GetProperty("accessToken").GetString()!;

        // 到期时间改到过去：作业还没跑、租户仍是生效，登录与刷新已被拒
        await SetExpiryAsync(id, DateTimeOffset.UtcNow.AddMinutes(-1));
        Assert.Equal("Active", (await TenantAsync(id)).GetProperty("status").GetString());
        var (expiredLogin, expiredLoginBody) = await LoginAsync();
        Assert.Equal(HttpStatusCode.Forbidden, expiredLogin.StatusCode);
        Assert.Equal("IDENTITY_DENIED_019", expiredLoginBody.GetProperty("code").GetString());
        Assert.Equal("租户 'fabrikam' 已到期，请联系平台管理员续期。", expiredLoginBody.GetProperty("detail").GetString());
        var (expiredRefresh, expiredRefreshBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/auth:refresh", $$"""{"refreshToken":"{{login.GetProperty("refreshToken").GetString()}}"}"""));
        Assert.Equal(HttpStatusCode.Forbidden, expiredRefresh.StatusCode);
        Assert.Equal("IDENTITY_DENIED_019", expiredRefreshBody.GetProperty("code").GetString());

        // 到期作业：标为到期、目录停用，带租户声明的调用被框架拒绝；登录仍按到期回答
        Assert.True((await RunJobAsync()).IsSuccess);
        Assert.Equal("Expired", (await TenantAsync(id)).GetProperty("status").GetString());
        var (blocked, blockedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/users:list", "{}", tenantToken));
        Assert.Equal(HttpStatusCode.Forbidden, blocked.StatusCode);
        Assert.Equal("MULTITENANCY_DENIED_001", blockedBody.GetProperty("code").GetString());
        Assert.Equal("IDENTITY_DENIED_019", (await LoginAsync()).Body.GetProperty("code").GetString());

        // 没延长就恢复：逐项失败，状态不变
        var (refused, refusedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants:resume", $$"""{"ids":["{{id}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, refused.StatusCode);
        Assert.Empty(refusedBody.GetProperty("succeeded").EnumerateArray());
        Assert.Equal("TENANCY_CONFLICT_015", refusedBody.GetProperty("failed")[0].GetProperty("code").GetString());
        Assert.Equal("Expired", (await TenantAsync(id)).GetProperty("status").GetString());

        // 延长之后还是到期状态（目录停用），恢复之后才能登录；作业不再动它
        await SetExpiryAsync(id, DateTimeOffset.UtcNow.AddDays(30));
        Assert.Equal("IDENTITY_DENIED_005", (await LoginAsync()).Body.GetProperty("code").GetString());
        var (resumed, resumedBody) = await server.SendAsync(BasicAppTestServer.Request(HttpMethod.Post, "/api/tenants:resume", $$"""{"ids":["{{id}}"]}""", admin));
        Assert.Equal(HttpStatusCode.OK, resumed.StatusCode);
        Assert.Equal([id], resumedBody.GetProperty("succeeded").EnumerateArray().Select(static e => e.GetString()).ToList());
        Assert.Equal("fabrikam", (await AuthFlowTests.LoginAsync(server, "admin", "Admin@123456", "fabrikam")).GetProperty("tenantId").GetString());
        Assert.True((await RunJobAsync()).IsSuccess);
        Assert.Equal("Active", (await TenantAsync(id)).GetProperty("status").GetString());
    }
}
